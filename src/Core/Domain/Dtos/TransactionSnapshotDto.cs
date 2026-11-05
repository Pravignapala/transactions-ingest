using System.Text.Json.Serialization;

namespace TransactionsIngest.Core.Domain.Dtos;

public record TransactionSnapshotDto(
    [property: JsonPropertyName("sourceSystem")] string SourceSystem,
    [property: JsonPropertyName("transactionId")] int TransactionId,
    [property: JsonPropertyName("cardNumber")] string? CardNumber,
    [property: JsonPropertyName("locationCode")] string LocationCode,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("transactionTime")] DateTime TransactionTime
);