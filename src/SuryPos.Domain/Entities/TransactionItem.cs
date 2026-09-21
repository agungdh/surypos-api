using System.ComponentModel.DataAnnotations.Schema;

namespace SuryPos.Domain.Entities;

public class TransactionItem
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [NotMapped]
    public decimal SubTotal => UnitPrice * Quantity;
}
