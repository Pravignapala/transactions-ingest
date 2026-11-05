using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TransactionsIngest.Core.Domain.Dtos;
using TransactionsIngest.Core.Domain.Entities;
using TransactionsIngest.Core.Domain.Enums;
using TransactionsIngest.Core.Domain.Interfaces;
using TransactionsIngest.Core.Services;
using TransactionsIngest.Infrastructure.Persistence;
using TransactionsIngest.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TransactionsIngest.UnitTests;

public class TransactionImportServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ExecuteImportAsync_ShouldInsertNewTransactions_AndRecordInsertAudit()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var repo = new TransactionRepository(db);

        var sampleSnapshot = new TransactionSnapshotDto(
            SourceSystem: "Store Uptown",
            TransactionId: 100001,
            CardNumber: "***1111",
            LocationCode: "UPTOWN",
            ProductName: "Wireless Mouse",
            Amount: 24.99m,
            TransactionTime: DateTime.UtcNow.AddHours(-2)
        );

        var mockAdapter = new Mock<ISourceSystemAdapter>();
        mockAdapter.SetupGet(a => a.SourceSystemName).Returns("Store Uptown");
        mockAdapter.Setup(a => a.FetchSnapshotAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<TransactionSnapshotDto> { sampleSnapshot });

        var service = new TransactionImportService(repo, new[] { mockAdapter.Object });

        // Act
        var result = await service.ExecuteImportAsync();

        // Assert
        result.InsertedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.RevokedCount.Should().Be(0);

        var saved = await db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == 100001 && t.SourceSystem == "Store Uptown");
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(TransactionStatus.Active);
        saved.Amount.Should().Be(24.99m);

        var logs = await db.AuditLogs.Where(a => a.TransactionId == 100001).ToListAsync();
        logs.Should().ContainSingle(l => l.Action == "INSERT");
    }

    [Fact]
    public async Task ExecuteImportAsync_ShouldBeIdempotent_WhenSnapshotIsUnchanged()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var repo = new TransactionRepository(db);

        var sampleSnapshot = new TransactionSnapshotDto(
            SourceSystem: "Online",
            TransactionId: 200001,
            CardNumber: "***4444",
            LocationCode: "ONLINE",
            ProductName: "USB-C Dock",
            Amount: 129.95m,
            TransactionTime: DateTime.UtcNow.AddHours(-1)
        );

        var mockAdapter = new Mock<ISourceSystemAdapter>();
        mockAdapter.SetupGet(a => a.SourceSystemName).Returns("Online");
        mockAdapter.Setup(a => a.FetchSnapshotAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<TransactionSnapshotDto> { sampleSnapshot });

        var service = new TransactionImportService(repo, new[] { mockAdapter.Object });

        // Act: Run First Import
        var firstResult = await service.ExecuteImportAsync();
        firstResult.InsertedCount.Should().Be(1);

        // Act: Run Second Import with exact same data
        var secondResult = await service.ExecuteImportAsync();

        // Assert: Zero inserts, zero updates, zero revokes
        secondResult.InsertedCount.Should().Be(0);
        secondResult.UpdatedCount.Should().Be(0);
        secondResult.RevokedCount.Should().Be(0);

        var logs = await db.AuditLogs.Where(a => a.TransactionId == 200001).ToListAsync();
        logs.Should().HaveCount(1, "running import multiple times without changes must not add redundant audit logs");
    }

    [Fact]
    public async Task ExecuteImportAsync_ShouldDetectFieldChanges_AndUpdateAndAudit()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var repo = new TransactionRepository(db);

        var initialTime = DateTime.UtcNow.AddHours(-5);
        var initialSnapshot = new TransactionSnapshotDto(
            SourceSystem: "Store Downtown",
            TransactionId: 300001,
            CardNumber: "***0002",
            LocationCode: "DOWNTOWN",
            ProductName: "Mechanical Keyboard",
            Amount: 89.99m,
            TransactionTime: initialTime
        );

        var mockAdapter = new Mock<ISourceSystemAdapter>();
        mockAdapter.SetupGet(a => a.SourceSystemName).Returns("Store Downtown");
        mockAdapter.SetupSequence(a => a.FetchSnapshotAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<TransactionSnapshotDto> { initialSnapshot })
                   .ReturnsAsync(new List<TransactionSnapshotDto>
                   {
                       // Modified amount and product
                       initialSnapshot with { Amount = 99.99m, ProductName = "RGB Keyboard" }
                   });

        var service = new TransactionImportService(repo, new[] { mockAdapter.Object });

        // Act
        await service.ExecuteImportAsync(); // First run
        var updateResult = await service.ExecuteImportAsync(); // Second run with changes

        // Assert
        updateResult.UpdatedCount.Should().Be(1);

        var tx = await db.Transactions.FirstAsync(t => t.TransactionId == 300001);
        tx.Status.Should().Be(TransactionStatus.Updated);
        tx.Amount.Should().Be(99.99m);
        tx.ProductName.Should().Be("RGB Keyboard");
        tx.RevisionNumber.Should().Be(2);

        var diffLogs = await db.AuditLogs
            .Where(a => a.TransactionId == 300001 && a.Action == "UPDATE")
            .ToListAsync();

        diffLogs.Should().HaveCount(2);
        diffLogs.Should().Contain(l => l.FieldName == "Amount" && l.OldValue == "89.99" && l.NewValue == "99.99");
        diffLogs.Should().Contain(l => l.FieldName == "ProductName" && l.OldValue == "Mechanical Keyboard" && l.NewValue == "RGB Keyboard");
    }

    [Fact]
    public async Task ExecuteImportAsync_ShouldRevokeTransaction_WhenMissingFromSnapshotWithin24Hours()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var repo = new TransactionRepository(db);

        var snapshot = new TransactionSnapshotDto(
            SourceSystem: "Store Uptown",
            TransactionId: 400001,
            CardNumber: "***9999",
            LocationCode: "UPTOWN",
            ProductName: "Desk Mat",
            Amount: 19.99m,
            TransactionTime: DateTime.UtcNow.AddHours(-10) // Occurred within past 24 hours
        );

        var mockAdapter = new Mock<ISourceSystemAdapter>();
        mockAdapter.SetupGet(a => a.SourceSystemName).Returns("Store Uptown");
        mockAdapter.SetupSequence(a => a.FetchSnapshotAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<TransactionSnapshotDto> { snapshot })
                   .ReturnsAsync(new List<TransactionSnapshotDto>()); // Empty snapshot simulates removal/revocation

        var service = new TransactionImportService(repo, new[] { mockAdapter.Object });

        // Act
        await service.ExecuteImportAsync(); // Initial insert
        var revokeResult = await service.ExecuteImportAsync(); // Snapshot drops transaction

        // Assert
        revokeResult.RevokedCount.Should().Be(1);

        var tx = await db.Transactions.FirstAsync(t => t.TransactionId == 400001);
        tx.Status.Should().Be(TransactionStatus.Revoked);

        var logs = await db.AuditLogs.Where(a => a.TransactionId == 400001 && a.Action == "REVOKE").ToListAsync();
        logs.Should().ContainSingle();
    }
}