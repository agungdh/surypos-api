using System.ComponentModel.DataAnnotations.Schema;

namespace SuryPos.Domain.Entities;

public class Transaction
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public List<TransactionItem> Items { get; set; } = [];

    [NotMapped]
    public decimal TotalAmount => Items.Sum(x => x.SubTotal);

    [NotMapped]
    public decimal TaxAmount => TotalAmount * 0.11m; // PPN 11%

    [NotMapped]
    public decimal GrandTotal => TotalAmount + TaxAmount;
}
