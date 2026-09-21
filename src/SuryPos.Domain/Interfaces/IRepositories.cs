using SuryPos.Domain.Entities;

namespace SuryPos.Domain.Interfaces;

public interface IProductRepository
{
    IEnumerable<Product> GetAll();
    Product? GetById(Guid id);
    void UpdateStock(Guid id, int qtyToReduce);
}

public interface ITransactionRepository
{
    void Save(Transaction transaction);
    IEnumerable<Transaction> GetAll();
}