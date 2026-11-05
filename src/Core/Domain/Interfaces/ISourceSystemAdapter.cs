using TransactionsIngest.Core.Domain.Dtos;

namespace TransactionsIngest.Core.Domain.Interfaces;

public interface ISourceSystemAdapter
{
    string SourceSystemName { get; }
    Task<IReadOnlyList<TransactionSnapshotDto>> FetchSnapshotAsync(CancellationToken cancellationToken = default);
}