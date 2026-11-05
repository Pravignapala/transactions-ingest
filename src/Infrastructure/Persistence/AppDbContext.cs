using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Core.Domain.Entities;

namespace TransactionsIngest.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transaction Entity Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Composite Primary Key: (SourceSystem, TransactionId)
            entity.HasKey(t => new { t.SourceSystem, t.TransactionId });

            entity.Property(t => t.SourceSystem)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.CardNumber)
                .HasMaxLength(20);

            entity.Property(t => t.LocationCode)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.ProductName)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.Amount)
                .HasPrecision(18, 2);

            entity.Property(t => t.TransactionTimeUtc)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Indexes for query performance
            entity.HasIndex(t => new { t.SourceSystem, t.TransactionTimeUtc });
            entity.HasIndex(t => t.Status);

            // Explicit 1-to-many relationship with composite foreign key
            entity.HasMany(t => t.AuditLogs)
                .WithOne()
                .HasForeignKey(a => new { a.SourceSystem, a.TransactionId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog Entity Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.SourceSystem)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(a => a.Action)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(a => a.FieldName)
                .HasMaxLength(50);

            entity.Property(a => a.BatchId)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(a => new { a.SourceSystem, a.TransactionId });
        });
    }
}