using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.DTOs;
using SuryPos.Service.Validators;

namespace SuryPos.Tests.Validators;

public class CheckoutValidationTests
{
    private static readonly Guid CoffeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class StubProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products = new()
        {
            [CoffeeId] = new Product { Id = CoffeeId, Name = "Kopi", Price = 18000, Stock = 50 },
        };

        public IEnumerable<Product> GetAll() => _products.Values;
        public Product? GetById(Guid id) => _products.GetValueOrDefault(id);
        public void UpdateStock(Guid id, int qtyToReduce) => _products[id].Stock -= qtyToReduce;
    }

    private static CheckoutRequestValidator CreateValidator()
        => new(new CheckoutItemDtoValidator(new StubProductRepository()));

    [Fact]
    public void NullItems_IsInvalid_WithSingleError()
    {
        var result = CreateValidator().Validate(new CheckoutRequestDto(null));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Items", error.PropertyName);
        Assert.Equal("Daftar item belanja wajib diisi.", error.ErrorMessage);
    }

    [Fact]
    public void EmptyItems_IsInvalid()
    {
        var result = CreateValidator().Validate(new CheckoutRequestDto([]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items" && e.ErrorMessage == "Keranjang belanja tidak boleh kosong.");
    }

    [Fact]
    public void EmptyProductId_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(Guid.Empty, 1)]);

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].ProductId" && e.ErrorMessage == "Product ID wajib diisi.");
    }

    [Fact]
    public void UnknownProduct_IsInvalid()
    {
        var request = new CheckoutRequestDto(
            [new CheckoutItemDto(Guid.Parse("99999999-9999-9999-9999-999999999999"), 1)]);

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].ProductId" && e.ErrorMessage.Contains("tidak ditemukan"));
    }

    [Fact]
    public void ZeroQuantity_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 0)]);

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].Quantity" && e.ErrorMessage == "Jumlah barang minimal 1 unit.");
    }

    [Fact]
    public void QuantityAboveStock_IsInvalid()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 51)]);

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Items[0].Quantity" && e.ErrorMessage.Contains("tidak mencukupi"));
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CheckoutRequestDto([new CheckoutItemDto(CoffeeId, 2)]);

        var result = CreateValidator().Validate(request);

        Assert.True(result.IsValid);
    }
}
