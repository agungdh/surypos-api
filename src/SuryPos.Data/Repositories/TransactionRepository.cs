using SuryPos.Domain.Entities;
using SuryPos.Domain.Interfaces;

namespace SuryPos.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private static readonly List<Transaction> _transactions = [];

    public void Save(Transaction transaction) => _transactions.Add(transaction);

    public IEnumerable<Transaction> GetAll() => _transactions;
}