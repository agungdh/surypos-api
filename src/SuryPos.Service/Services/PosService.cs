using FluentValidation;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;

namespace SuryPos.Service.Services;

public interface IPosService
{
    IEnumerable<ProductDto> GetProducts();
    TransactionResponseDto Checkout(CheckoutRequestDto request);
}

public class PosService(
    IProductRepository productRepo, 
    ITransactionRepository transactionRepo,
    IValidator<CheckoutRequestDto> checkoutValidator) : IPosService
{
    public IEnumerable<ProductDto> GetProducts()
    {
        return productRepo.GetAll()
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock));
    }

    public TransactionResponseDto Checkout(CheckoutRequestDto request)
    {
        // 1. Eksekusi FluentValidation
        var validationResult = checkoutValidator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Logika Bisnis In-Memory
        var transaction = new Transaction
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        foreach (var item in request.Items)
        {
            var product = productRepo.GetById(item.ProductId)
                ?? throw new KeyNotFoundException($"Produk dengan ID '{item.ProductId}' tidak ditemukan!");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException($"Stok produk '{product.Name}' tidak mencukupi! Sisa stok: {product.Stock}");

            // Potong stok produk
            productRepo.UpdateStock(product.Id, item.Quantity);

            // Tambahkan ke rincian nota
            transaction.Items.Add(new TransactionItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        // Simpan nota ke repository
        transactionRepo.Save(transaction);

        // Map ke Response DTO
        var itemDtos = transaction.Items
            .Select(i => new TransactionItemDto(i.ProductName, i.UnitPrice, i.Quantity, i.SubTotal))
            .ToList();

        return new TransactionResponseDto(
            transaction.InvoiceNumber,
            transaction.Date,
            itemDtos,
            transaction.TotalAmount,
            transaction.TaxAmount,
            transaction.GrandTotal
        );
    }
}