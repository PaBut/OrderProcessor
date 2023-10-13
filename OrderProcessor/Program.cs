using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderProcessor.DatabaseContext;
using OrderProcessor.Entities;
using OrderProcessor.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using OrderProcessor.Models.Common;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false).Build();

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql(configuration.GetConnectionString("default"));

ApplicationDbContext dbContext = new ApplicationDbContext(optionsBuilder.Options);

ConnectionFactory factory = new ConnectionFactory();

factory.Uri = new Uri(configuration["RabbitMqSettings:url"] ?? throw new ArgumentNullException());
factory.ClientProvidedName = "OrderProcessor";

IConnection readerConnection = factory.CreateConnection();
IConnection publisherConnection = factory.CreateConnection();

string exchange = configuration["RabbitMqSettings:exchangeName"] ?? throw new ArgumentNullException();
string queue = configuration["RabbitMqSettings:queueName"] ?? throw new ArgumentNullException();
string routingKey = configuration["RabbitMqSettings:routingKey"] ?? throw new ArgumentNullException();

IModel channelReceiver = InitializeChannel(readerConnection);
channelReceiver.BasicQos(0, 1, false);

IModel channelSender = InitializeChannel(publisherConnection);

var consumer = new EventingBasicConsumer(channelReceiver);

consumer.Received += ProcessMessage;

var consumerTag = channelReceiver.BasicConsume(queue, false, consumer);

Console.ReadLine();

dbContext.Dispose();
channelReceiver.BasicCancel(consumerTag);
channelReceiver.Close();
channelSender.Close();
readerConnection.Close();
publisherConnection.Close();

IModel InitializeChannel(IConnection connection)
{
    IModel channel = connection.CreateModel();
    channel.ExchangeDeclare(exchange, ExchangeType.Direct);
    channel.QueueDeclare(queue, false, false, false, null);
    channel.QueueBind(queue, exchange, routingKey, null);
    return channel;
}

void ProcessMessage(object? sender, BasicDeliverEventArgs args)
{
    var msgTypeHeader = args.BasicProperties.Headers["X-MsgType"];

    if (msgTypeHeader == null || msgTypeHeader is not byte[])
    {
        channelReceiver.BasicReject(args.DeliveryTag, false);
        return;
    }

    var typeString = Encoding.UTF8.GetString((msgTypeHeader as byte[])!);

    var assembly = typeof(OrderEvent).Assembly;

    var type = assembly.GetType(typeof(OrderEvent).Namespace + "." + typeString);

    if (type == null)
    {
        channelReceiver.BasicReject(args.DeliveryTag, false);
        return;
    }

    var bodyBytes = args.Body.ToArray();

    var model = JsonSerializer.Deserialize(bodyBytes, type);

    if (model is IValidatable validatable)
    {
        if (!validatable.IsValid())
        {
            channelReceiver.BasicReject(args.DeliveryTag, false);
            return;
        }
    }
    else
    {
        channelReceiver.BasicReject(args.DeliveryTag, false);
        return;
    }

    if (model is OrderEvent orderModel)
    {
        if (dbContext.Orders.Any(o => o.Id == orderModel.Id))
        {
            channelReceiver.BasicReject(args.DeliveryTag, false);
            return;
        }

        var order = Order.FromOrderEvent(orderModel);

        dbContext.Orders.Add(order);
        dbContext.SaveChanges();
        channelReceiver.BasicAck(args.DeliveryTag, false);
    }
    else if (model is PaymentEvent paymentModel)
    {
        var order = dbContext.Orders.FirstOrDefault(o => o.Id == paymentModel.OrderId);

        if (order == null)
        {
            channelReceiver.BasicNack(args.DeliveryTag, false, false);
            //Sending PaymentEvent message to the end of the queue
            channelSender.BasicPublish(exchange, routingKey, args.BasicProperties, args.Body);

            return;
        }

        if (order.AmountPaid + paymentModel.Amount >= order.Total && !order.IsPaid)
        {
            Console.WriteLine($"Order: {order.Id}, Product: {order.Product}, Total: {order.Total}, Status: PAID");
        }

        order.AmountPaid += paymentModel.Amount;

        dbContext.SaveChanges();

        channelReceiver.BasicAck(args.DeliveryTag, false);
    }
    else
    {
        channelReceiver.BasicReject(args.DeliveryTag, false);
    }
}