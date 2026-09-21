using FluentValidation;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;
using SuryPos.Service.Services;
using SuryPos.Service.Validators;

namespace SuryPos.Service.Tests.Services;

public class PosServiceTests
{
    private static readonly Guid CoffeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class StubProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products;

        public StubProductRepository(bool withCoffee = true)
        {
            _products = withCoffee
                ? new Dictionary<Guid, Product>
                {
                    [CoffeeId] = new Product { Id = CoffeeId, Name = "Kopi", Price = 18000, Stock = 50 },
                }
                : [];
        }

        public IEnumerable<Product> GetAll() => _products.Values;
        public Product? GetById(Guid id) => _products.GetValueOrDefault(id);
        public void UpdateStock(Guid id, int qtyToReduce) => _products[id].Stock -= qtyToReduce;
        public int StockOf(Guid id) => _products[id].Stock;
    }

    private sealed class StubTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Saved { get; } = [];
        public IEnumerable<Transaction> GetAll() => Saved;
        public void Save(Transaction transaction) => Saved.Add(transaction);
    }

    private static PosService CreateService(
        StubProductRepository productRepo, StubTransactionRepository transactionRepo)
    {
        IValidator<CheckoutRequestDto> validator =
            new CheckoutRequestValidator(new CheckoutItemDtoValidator(productRepo));
        return new PosService(productRepo, transactionRepo, validator);
    }

    [Fact]
    public void Checkout_ValidRequest_ReturnsTotalsReducesStockAndSaves()
    {
        var products = new StubProductRepository();
        var transactions = new StubTransactionRepository();
        var service = CreateService(products, transactions);

        var result = service.Checkout(new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 2)]));

        Assert.Equal(36000, result.TotalAmount);
        Assert.Equal(3960, result.TaxAmount);
        Assert.Equal(39960, result.GrandTotal);
        Assert.StartsWith("INV-", result.InvoiceNumber);
        Assert.Equal(48, products.StockOf(CoffeeId));
        Assert.Single(transactions.Saved);
    }

    [Fact]
    public void Checkout_EmptyItems_ThrowsValidationException()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());

        Assert.Throws<ValidationException>(
            () => service.Checkout(new CheckoutRequestDto([])));
    }

    [Fact]
    public void Checkout_UnknownProduct_ThrowsValidationException_NotKeyNotFound()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());
        var request = new CheckoutRequestDto(
            [new CheckoutItemDto(Guid.Parse("99999999-9999-9999-9999-999999999999"), 1)]);

        var ex = Assert.Throws<ValidationException>(() => service.Checkout(request));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Items[0].ProductId");
    }

    [Fact]
    public void Checkout_Overstock_ThrowsValidationException()
    {
        var service = CreateService(new StubProductRepository(), new StubTransactionRepository());
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 51)]);

        var ex = Assert.Throws<ValidationException>(() => service.Checkout(request));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Items[0].Quantity");
    }

    [Fact]
    public void Checkout_ProductVanishesAfterValidation_BecomesServerError()
    {
        // Validator melihat produk ada, tapi repo service tidak punya -> race/gangguan.
        // Service tidak boleh mengubahnya jadi 4xx; biar meledak sebagai 5xx.
        var validatorRepo = new StubProductRepository(withCoffee: true);
        var serviceRepo = new StubProductRepository(withCoffee: false);
        IValidator<CheckoutRequestDto> validator =
            new CheckoutRequestValidator(new CheckoutItemDtoValidator(validatorRepo));
        var service = new PosService(serviceRepo, new StubTransactionRepository(), validator);
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 1)]);

        Assert.ThrowsAny<Exception>(() => service.Checkout(request));
    }
}
