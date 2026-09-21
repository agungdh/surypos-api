using SuryPos.Domain.Entities;

namespace SuryPos.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <returns>
    /// True jika stok berhasil dikurangi. False jika produk tidak ada
    /// atau stok tidak mencukupi (kondisi balapan / validasi basi).
    /// </returns>
    Task<bool> TryDecreaseStockAsync(long id, int qtyToReduce, CancellationToken ct = default);
}

public interface ITransactionRepository
{
    Task SaveAsync(Transaction transaction, CancellationToken ct = default);
    Task<List<Transaction>> GetAllAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken ct = default);
}
