namespace SuryPos.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public List<TransactionItem> Items { get; set; } = [];
    public decimal TotalAmount => Items.Sum(x => x.SubTotal);
    public decimal TaxAmount => TotalAmount * 0.11m; // PPN 11%
    public decimal GrandTotal => TotalAmount + TaxAmount;
}