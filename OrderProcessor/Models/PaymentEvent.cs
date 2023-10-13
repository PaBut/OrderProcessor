using OrderProcessor.Models.Common;

namespace OrderProcessor.Models;

public class PaymentEvent : IValidatable
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public bool IsValid() =>
        !string.IsNullOrEmpty(OrderId)
        && Amount > 0;
}