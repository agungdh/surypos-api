using Microsoft.EntityFrameworkCore;
using SuryPos.Domain.Interfaces;

namespace SuryPos.Data;

public class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        => await ExecuteInTransactionAsync<object?>(async c => { await action(c); return null; }, ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken ct = default)
    {
        // Kalau sudah di dalam transaksi (nested), langsung jalan tanpa transaksi baru.
        if (db.Database.CurrentTransaction is not null)
            return await func(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await func(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
