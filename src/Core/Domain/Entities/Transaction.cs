using TransactionsIngest.Core.Domain.Enums;

namespace TransactionsIngest.Core.Domain.Entities;

public class Transaction
{
    // Composite Key: (SourceSystem, TransactionId)
    public string SourceSystem { get; set; } = string.Empty;
    public int TransactionId { get; set; }

    public string? CardNumber { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionTimeUtc { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int RevisionNumber { get; set; } = 1;

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}