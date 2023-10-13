using System.ComponentModel.DataAnnotations;
using OrderProcessor.Models;

namespace OrderProcessor.Entities;

public class Order
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = string.Empty;

    public bool IsPaid => AmountPaid >= Total;

    public static Order FromOrderEvent(OrderEvent orderEvent) => new()
    {
        Id = orderEvent.Id,
        Product = orderEvent.Product,
        Total = orderEvent.Total,
        AmountPaid = 0,
        Currency = orderEvent.Currency
    };
}