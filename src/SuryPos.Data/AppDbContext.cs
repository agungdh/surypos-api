using Microsoft.EntityFrameworkCore;
using SuryPos.Domain.Entities;

namespace SuryPos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("numeric(18,2)");
            entity.ToTable(t => t.HasCheckConstraint("ck_products_stock_non_negative", "stock >= 0"));

            entity.HasData(
                new Product { Id = 1, Name = "Kopi Susu Gula Aren", Price = 18000, Stock = 50 },
                new Product { Id = 2, Name = "Croissant Cokelat", Price = 25000, Stock = 20 },
                new Product { Id = 3, Name = "Air Mineral", Price = 5000, Stock = 100 });
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(32);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.Date).HasColumnType("timestamptz");
            entity.Ignore(e => e.TotalAmount);
            entity.Ignore(e => e.TaxAmount);
            entity.Ignore(e => e.GrandTotal);
        });

        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Ignore(e => e.SubTotal);
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
