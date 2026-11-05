namespace TransactionsIngest.Core.Domain.Interfaces;

public record ImportResult(
    string BatchId,
    int InsertedCount,
    int UpdatedCount,
    int RevokedCount,
    DateTime CompletedAtUtc
);

public interface IImportService
{
    Task<ImportResult> ExecuteImportAsync(CancellationToken cancellationToken = default);
}