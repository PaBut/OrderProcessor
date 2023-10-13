using System.ComponentModel.DataAnnotations;
using OrderProcessor.Models.Common;

namespace OrderProcessor.Models;

public class OrderEvent : IValidatable
{
    public string Id { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;

    public bool IsValid() =>
        !string.IsNullOrEmpty(Id)
        && !string.IsNullOrEmpty(Product)
        && Total > 0
        && !string.IsNullOrEmpty(Currency);
}