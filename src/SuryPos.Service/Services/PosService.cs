using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;

namespace SuryPos.Service.Services;

public interface IPosService
{
    IEnumerable<ProductDto> GetProducts();
    TransactionResponseDto Checkout(CheckoutRequestDto request);
}

public class PosService : IPosService
{
    private readonly IProductRepository _productRepo;
    private readonly ITransactionRepository _transactionRepo;

    public PosService(IProductRepository productRepo, ITransactionRepository transactionRepo)
    {
        _productRepo = productRepo;
        _transactionRepo = transactionRepo;
    }

    public IEnumerable<ProductDto> GetProducts()
    {
        return _productRepo.GetAll()
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock));
    }

    public TransactionResponseDto Checkout(CheckoutRequestDto request)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Keranjang belanja tidak boleh kosong!");

        var transaction = new Transaction
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        foreach (var item in request.Items)
        {
            var product = _productRepo.GetById(item.ProductId)
                ?? throw new KeyNotFoundException($"Produk dengan ID '{item.ProductId}' tidak ditemukan!");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException($"Stok produk '{product.Name}' tidak mencukupi! Sisa stok: {product.Stock}");

            // Potong stok produk
            _productRepo.UpdateStock(product.Id, item.Quantity);

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
        _transactionRepo.Save(transaction);

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