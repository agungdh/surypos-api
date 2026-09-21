namespace SuryPos.Service.DTOs;

public record ProductDto(long Id, string Name, decimal Price, int Stock);

public record TransactionItemDto(string ProductName, decimal UnitPrice, int Quantity, decimal SubTotal);

public record TransactionResponseDto(
    string InvoiceNumber,
    DateTime Date,
    List<TransactionItemDto> Items,
    decimal TotalAmount,
    decimal TaxAmount,
    decimal GrandTotal
);

public record CheckoutItemDto(long ProductId, int Quantity);
public record CheckoutRequestDto(List<CheckoutItemDto>? Items);
