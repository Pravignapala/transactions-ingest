using TransactionsIngest.Core.Domain.Dtos;
using TransactionsIngest.Core.Domain.Entities;
using TransactionsIngest.Core.Domain.Enums;
using TransactionsIngest.Core.Domain.Interfaces;

namespace TransactionsIngest.Core.Services;

public class TransactionImportService(
    ITransactionRepository repository,
    IEnumerable<ISourceSystemAdapter> adapters) : IImportService
{
    public async Task<ImportResult> ExecuteImportAsync(CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid().ToString("N");
        var nowUtc = DateTime.UtcNow;
        var revocationCutoffUtc = nowUtc.AddHours(-24);

        int totalInserted = 0;
        int totalUpdated = 0;
        int totalRevoked = 0;

        foreach (var adapter in adapters)
        {
            var systemName = adapter.SourceSystemName;
            var incomingSnapshots = await adapter.FetchSnapshotAsync(cancellationToken);
            var snapshotLookup = incomingSnapshots.ToDictionary(s => s.TransactionId);

            // 1. Fetch all existing records for this source system
            var existingTransactions = await repository.GetActiveOrRecentBySourceSystemAsync(systemName, cancellationToken);
            var existingLookup = existingTransactions.ToDictionary(t => t.TransactionId);

            // 2. Process incoming snapshots (Inserts & Updates)
            foreach (var incoming in incomingSnapshots)
            {
                if (!existingLookup.TryGetValue(incoming.TransactionId, out var existing))
                {
                    // Brand new transaction
                    var newEntity = new Transaction
                    {
                        SourceSystem = systemName,
                        TransactionId = incoming.TransactionId,
                        CardNumber = incoming.CardNumber,
                        LocationCode = incoming.LocationCode,
                        ProductName = incoming.ProductName,
                        Amount = incoming.Amount,
                        TransactionTimeUtc = incoming.TransactionTime,
                        // If record is already older than 24h when received, start as Finalized; otherwise Active
                        Status = incoming.TransactionTime < revocationCutoffUtc 
                            ? TransactionStatus.Finalized 
                            : TransactionStatus.Active,
                        CreatedAtUtc = nowUtc,
                        UpdatedAtUtc = nowUtc,
                        RevisionNumber = 1
                    };

                    var insertLog = new AuditLog
                    {
                        SourceSystem = systemName,
                        TransactionId = incoming.TransactionId,
                        Action = "INSERT",
                        BatchId = batchId,
                        ChangedAtUtc = nowUtc
                    };

                    await repository.AddTransactionAsync(newEntity, insertLog, cancellationToken);
                    totalInserted++;
                }
                else
                {
                    // Existing record: Finalized or Revoked records are immutable against snapshots
                    if (existing.Status is TransactionStatus.Finalized or TransactionStatus.Revoked)
                    {
                        continue;
                    }

                    // Compute field-level differences for change detection
                    var diffs = ComputeDifferences(existing, incoming);
                    if (diffs.Count > 0)
                    {
                        var updateLogs = new List<AuditLog>();
                        foreach (var (field, oldVal, newVal) in diffs)
                        {
                            updateLogs.Add(new AuditLog
                            {
                                SourceSystem = systemName,
                                TransactionId = existing.TransactionId,
                                Action = "UPDATE",
                                FieldName = field,
                                OldValue = oldVal,
                                NewValue = newVal,
                                BatchId = batchId,
                                ChangedAtUtc = nowUtc
                            });
                        }

                        // Apply updates
                        existing.CardNumber = incoming.CardNumber;
                        existing.LocationCode = incoming.LocationCode;
                        existing.ProductName = incoming.ProductName;
                        existing.Amount = incoming.Amount;
                        existing.TransactionTimeUtc = incoming.TransactionTime;
                        existing.Status = TransactionStatus.Updated;
                        existing.UpdatedAtUtc = nowUtc;
                        existing.RevisionNumber++;

                        await repository.UpdateTransactionAsync(existing, updateLogs, cancellationToken);
                        totalUpdated++;
                    }
                    // If diffs.Count == 0, strictly no-op (Idempotency preserved)
                }
            }

            // 3. Revocation: Missing from snapshot within 24h window
            var revocationCandidates = existingTransactions
                .Where(t => t.Status is TransactionStatus.Active or TransactionStatus.Updated)
                .Where(t => t.TransactionTimeUtc >= revocationCutoffUtc);

            foreach (var candidate in revocationCandidates)
            {
                if (!snapshotLookup.ContainsKey(candidate.TransactionId))
                {
                    candidate.Status = TransactionStatus.Revoked;
                    candidate.UpdatedAtUtc = nowUtc;

                    var revokeLog = new AuditLog
                    {
                        SourceSystem = systemName,
                        TransactionId = candidate.TransactionId,
                        Action = "REVOKE",
                        OldValue = "Active/Updated",
                        NewValue = "Revoked",
                        BatchId = batchId,
                        ChangedAtUtc = nowUtc
                    };

                    await repository.UpdateTransactionAsync(candidate, [revokeLog], cancellationToken);
                    totalRevoked++;
                }
            }

            // 4. Finalization: Records reaching 24h maturity become immutable
            var matureCandidates = existingTransactions
                .Where(t => t.Status is TransactionStatus.Active or TransactionStatus.Updated)
                .Where(t => t.TransactionTimeUtc < revocationCutoffUtc);

            foreach (var mature in matureCandidates)
            {
                mature.Status = TransactionStatus.Finalized;
                mature.UpdatedAtUtc = nowUtc;
                await repository.UpdateTransactionAsync(mature, [], cancellationToken);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);

        return new ImportResult(batchId, totalInserted, totalUpdated, totalRevoked, nowUtc);
    }

    private static List<(string Field, string OldVal, string NewVal)> ComputeDifferences(
        Transaction current, TransactionSnapshotDto incoming)
    {
        var diffs = new List<(string, string, string)>();

        if ((current.CardNumber ?? string.Empty) != (incoming.CardNumber ?? string.Empty))
            diffs.Add((nameof(Transaction.CardNumber), current.CardNumber ?? "", incoming.CardNumber ?? ""));

        if (current.LocationCode != incoming.LocationCode)
            diffs.Add((nameof(Transaction.LocationCode), current.LocationCode, incoming.LocationCode));

        if (current.ProductName != incoming.ProductName)
            diffs.Add((nameof(Transaction.ProductName), current.ProductName, incoming.ProductName));

        if (current.Amount != incoming.Amount)
            diffs.Add((nameof(Transaction.Amount), current.Amount.ToString("F2"), incoming.Amount.ToString("F2")));

        if (current.TransactionTimeUtc != incoming.TransactionTime)
            diffs.Add((nameof(Transaction.TransactionTimeUtc), current.TransactionTimeUtc.ToString("o"), incoming.TransactionTime.ToString("o")));

        return diffs;
    }
}