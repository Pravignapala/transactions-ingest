using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Core.Domain.Entities;
using TransactionsIngest.Core.Domain.Interfaces;

namespace TransactionsIngest.Infrastructure.Persistence.Repositories;

public class TransactionRepository(AppDbContext dbContext) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetActiveOrRecentBySourceSystemAsync(
        string sourceSystem, 
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions
            .Where(t => t.SourceSystem == sourceSystem)
            .ToListAsync(cancellationToken);
    }

    public Task AddTransactionAsync(
        Transaction transaction, 
        AuditLog auditLog, 
        CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Add(transaction);
        dbContext.AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task UpdateTransactionAsync(
        Transaction transaction, 
        IEnumerable<AuditLog> auditLogs, 
        CancellationToken cancellationToken = default)
    {
        dbContext.Transactions.Update(transaction);
        dbContext.AuditLogs.AddRange(auditLogs);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}