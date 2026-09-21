using Microsoft.EntityFrameworkCore;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;

namespace SuryPos.Data.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<List<Product>> GetAllAsync(CancellationToken ct = default)
        => db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

    public Task<Product?> GetByIdAsync(long id, CancellationToken ct = default)
        => db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> TryDecreaseStockAsync(long id, int qtyToReduce, CancellationToken ct = default)
    {
        var affected = await db.Products
            .Where(p => p.Id == id && p.Stock >= qtyToReduce)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.Stock, p => p.Stock - qtyToReduce),
                ct);
        return affected == 1;
    }
}
