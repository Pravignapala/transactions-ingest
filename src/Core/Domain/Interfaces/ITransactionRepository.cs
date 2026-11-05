using TransactionsIngest.Core.Domain.Entities;

namespace TransactionsIngest.Core.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetActiveOrRecentBySourceSystemAsync(string sourceSystem, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(Transaction transaction, AuditLog auditLog, CancellationToken cancellationToken = default);
    Task UpdateTransactionAsync(Transaction transaction, IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}