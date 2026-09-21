using Microsoft.EntityFrameworkCore;
using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;

namespace SuryPos.Data.Repositories;

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task SaveAsync(Transaction transaction, CancellationToken ct = default)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<Transaction>> GetAllAsync(CancellationToken ct = default)
        => db.Transactions.AsNoTracking()
            .Include(t => t.Items)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);
}
