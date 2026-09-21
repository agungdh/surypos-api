using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;

namespace SuryPos.Data.Repositories;

public class ProductRepository : IProductRepository
{
    // Dummy data produk kasir untuk pengujian
    private static readonly List<Product> _products =
    [
        new Product { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Kopi Susu Gula Aren", Price = 18000, Stock = 50 },
        new Product { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Croissant Cokelat", Price = 25000, Stock = 20 },
        new Product { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Air Mineral", Price = 5000, Stock = 100 }
    ];

    public IEnumerable<Product> GetAll() => _products;

    public Product? GetById(Guid id) => _products.FirstOrDefault(p => p.Id == id);

    public void UpdateStock(Guid id, int qtyToReduce)
    {
        var product = GetById(id);
        product?.Stock -= qtyToReduce;
    }
}