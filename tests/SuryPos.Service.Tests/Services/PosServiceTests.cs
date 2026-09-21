using FluentValidation;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;
using SuryPos.Service.Services;
using SuryPos.Service.Validators;

namespace SuryPos.Service.Tests.Services;

public class PosServiceTests
{
    private const long CoffeeId = 1;

    private sealed class StubProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _products;

        public StubProductRepository(bool withCoffee = true)
        {
            _products = withCoffee
                ? new Dictionary<long, Product>
                {
                    [CoffeeId] = new Product { Id = CoffeeId, Name = "Kopi", Price = 18000, Stock = 50 },
                }
                : [];
        }

        public Task<List<Product>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_products.Values.ToList());

        public Task<Product?> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(_products.GetValueOrDefault(id));

        public Task<bool> TryDecreaseStockAsync(long id, int qtyToReduce, CancellationToken ct = default)
        {
            if (!_products.TryGetValue(id, out var product) || product.Stock < qtyToReduce)
                return Task.FromResult(false);
            product.Stock -= qtyToReduce;
            return Task.FromResult(true);
        }

        public int StockOf(long id) => _products[id].Stock;
    }

    private sealed class StubTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Saved { get; } = [];
        public Task<List<Transaction>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Saved);
        public Task SaveAsync(Transaction transaction, CancellationToken ct = default)
        {
            Saved.Add(transaction);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
            => action(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken ct = default)
            => func(ct);
    }

    private static PosService CreateService(
        StubProductRepository productRepo, StubTransactionRepository transactionRepo)
    {
        IValidator<CheckoutRequestDto> validator =
            new CheckoutRequestValidator(new CheckoutItemDtoValidator(productRepo));
        return new PosService(productRepo, transactionRepo, new StubUnitOfWork(), validator);
    }

    [Fact]
    public async Task Checkout_ValidRequest_ReturnsTotalsReducesStockAndSaves()
    {
        var products = new StubProductRepository();
        var transactions = new StubTransactionRepository();
        var service = CreateService(products, transactions);

        var result = await service.CheckoutAsync(new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 2)]));

        Assert.Equal(36000, result.TotalAmount);
        Assert.Equal(3960, result.TaxAmount);
        Assert.Equal(39960, result.GrandTotal);
        Assert.StartsWith("INV-", result.InvoiceNumber);
        Assert.Equal(48, products.StockOf(CoffeeId));
        Assert.Single(transactions.Saved);
    }

    [Fact]
    public async Task Checkout_EmptyItems_ThrowsValidationException()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CheckoutAsync(new CheckoutRequestDto([])));
    }

    [Fact]
    public async Task Checkout_UnknownProduct_ThrowsValidationException_NotKeyNotFound()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());
        var request = new CheckoutRequestDto([new CheckoutItemDto(9999, 1)]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CheckoutAsync(request));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Items[0].ProductId");
    }

    [Fact]
    public async Task Checkout_Overstock_ThrowsValidationException()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 51)]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CheckoutAsync(request));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Items[0].Quantity");
    }

    [Fact]
    public async Task Checkout_ProductVanishesAfterValidation_BecomesServerError()
    {
        // Validator melihat produk ada, tapi repo service tidak punya -> race/gangguan.
        // Service tidak boleh mengubahnya jadi 4xx; biar meledak sebagai 5xx.
        var validatorRepo = new StubProductRepository(withCoffee: true);
        var serviceRepo = new StubProductRepository(withCoffee: false);
        IValidator<CheckoutRequestDto> validator =
            new CheckoutRequestValidator(new CheckoutItemDtoValidator(validatorRepo));
        var service = new PosService(serviceRepo, new StubTransactionRepository(), new StubUnitOfWork(), validator);
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 1)]);

        await Assert.ThrowsAnyAsync<Exception>(() => service.CheckoutAsync(request));
    }
}
