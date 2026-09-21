using FluentValidation;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;

namespace SuryPos.Service.Services;

public interface IPosService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync(CancellationToken ct = default);
    Task<TransactionResponseDto> CheckoutAsync(CheckoutRequestDto request, CancellationToken ct = default);
}

public class PosService(
    IProductRepository productRepo,
    ITransactionRepository transactionRepo,
    IUnitOfWork unitOfWork,
    IValidator<CheckoutRequestDto> checkoutValidator) : IPosService
{
    public async Task<IEnumerable<ProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var products = await productRepo.GetAllAsync(ct);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock));
    }

    public async Task<TransactionResponseDto> CheckoutAsync(CheckoutRequestDto request, CancellationToken ct = default)
    {
        // 1. Eksekusi FluentValidation
        var validationResult = await checkoutValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Semua tulis DB dalam satu transaksi: kurang stok + simpan nota.
        return await unitOfWork.ExecuteInTransactionAsync(async c =>
        {
            var transaction = new Transaction
            {
                // Milidetik + random agar nomor unik walau checkout bersamaan
                // (kolom invoice_number unique di Postgres).
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(1000, 10000)}"
            };

            foreach (var item in request.Items!)
            {
                // Validator menjamin produk ada & stok cukup.
                // Kalau di sini null, berarti kondisi balapan/gangguan DB -> biar jadi 5xx.
                var product = await productRepo.GetByIdAsync(item.ProductId, c)
                    ?? throw new InvalidOperationException(
                        $"Produk dengan ID '{item.ProductId}' tidak ditemukan saat checkout!");

                // Decrement atomik: hanya berhasil jika stok >= qty.
                // False = stok habis di tengah jalan (balapan) -> 5xx, jangan diam-diam.
                var decreased = await productRepo.TryDecreaseStockAsync(product.Id, item.Quantity, c);
                if (!decreased)
                    throw new InvalidOperationException(
                        $"Stok produk '{product.Name}' tidak mencukupi saat checkout!");

                transaction.Items.Add(new TransactionItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                });
            }

            await transactionRepo.SaveAsync(transaction, c);

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
        }, ct);
    }
}
