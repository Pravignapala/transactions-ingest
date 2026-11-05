namespace TransactionsIngest.Core.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public int TransactionId { get; set; }
    public string Action { get; set; } = string.Empty; // "INSERT", "UPDATE", "REVOKE"
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string BatchId { get; set; } = string.Empty;
}