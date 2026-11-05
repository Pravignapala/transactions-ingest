using TransactionsIngest.Core.Domain.Dtos;
using TransactionsIngest.Core.Domain.Interfaces;

namespace TransactionsIngest.Infrastructure.External;

public class MockUptownAdapter : ISourceSystemAdapter
{
    public string SourceSystemName => "Store Uptown";

    public Task<IReadOnlyList<TransactionSnapshotDto>> FetchSnapshotAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TransactionSnapshotDto> data =
        [
            new(
                SourceSystem: "Store Uptown",
                TransactionId: 100001,
                CardNumber: "***1111",
                LocationCode: "UPTOWN",
                ProductName: "Wireless Mouse",
                Amount: 24.99m,
                TransactionTime: DateTime.Parse("2026-02-27T08:15:30Z").ToUniversalTime()
            )
        ];

        return Task.FromResult(data);
    }
}

public class MockDowntownAdapter : ISourceSystemAdapter
{
    public string SourceSystemName => "Store Downtown";

    public Task<IReadOnlyList<TransactionSnapshotDto>> FetchSnapshotAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TransactionSnapshotDto> data =
        [
            new(
                SourceSystem: "Store Downtown",
                TransactionId: 100001,
                CardNumber: "***0002",
                LocationCode: "DOWNTOWN",
                ProductName: "Mechanical Keyboard",
                Amount: 89.99m,
                TransactionTime: DateTime.Parse("2026-02-27T09:45:12Z").ToUniversalTime()
            )
        ];

        return Task.FromResult(data);
    }
}

public class MockOnlineAdapter : ISourceSystemAdapter
{
    public string SourceSystemName => "Online";

    public Task<IReadOnlyList<TransactionSnapshotDto>> FetchSnapshotAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TransactionSnapshotDto> data =
        [
            new(
                SourceSystem: "Online",
                TransactionId: 100001,
                CardNumber: "***4444",
                LocationCode: "ONLINE",
                ProductName: "USB-C Docking Station",
                Amount: 129.95m,
                TransactionTime: DateTime.Parse("2026-02-27T11:20:05Z").ToUniversalTime()
            )
        ];

        return Task.FromResult(data);
    }
}