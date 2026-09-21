using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;
using SuryPos.Service.Validators;

namespace SuryPos.Service.Tests.Validators;

public class CheckoutValidationTests
{
    private const long CoffeeId = 1;

    private sealed class StubProductRepository : IProductRepository
    {
        private readonly Dictionary<long, Product> _products = new()
        {
            [CoffeeId] = new Product { Id = CoffeeId, Name = "Kopi", Price = 18000, Stock = 50 },
        };

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
    }

    private static CheckoutRequestValidator CreateValidator()
        => new(new CheckoutItemDtoValidator(new StubProductRepository()));

    [Fact]
    public async Task NullItems_IsInvalid_WithSingleError()
    {
        var result = await CreateValidator().ValidateAsync(new CheckoutRequestDto(null));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Items", error.PropertyName);
        Assert.Equal("Daftar item belanja wajib diisi.", error.ErrorMessage);
    }

    [Fact]
    public async Task EmptyItems_IsInvalid()
    {
        var result = await CreateValidator().ValidateAsync(new CheckoutRequestDto([]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items" && e.ErrorMessage == "Keranjang belanja tidak boleh kosong.");
    }

    [Fact]
    public async Task EmptyProductId_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(0, 1)]);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].ProductId" && e.ErrorMessage == "Product ID wajib diisi.");
    }

    [Fact]
    public async Task UnknownProduct_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(9999, 1)]);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].ProductId" && e.ErrorMessage.Contains("tidak ditemukan"));
    }

    [Fact]
    public async Task ZeroQuantity_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 0)]);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].Quantity" && e.ErrorMessage == "Jumlah barang minimal 1 unit.");
    }

    [Fact]
    public async Task QuantityAboveStock_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 51)]);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].Quantity" && e.ErrorMessage.Contains("tidak mencukupi"));
    }

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 2)]);

        var result = await CreateValidator().ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
