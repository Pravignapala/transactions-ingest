using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Core.Domain.Entities;
using TransactionsIngest.Core.Domain.Enums;
using TransactionsIngest.Core.Domain.Interfaces;
using TransactionsIngest.Core.Services;
using TransactionsIngest.Infrastructure.External;
using TransactionsIngest.Infrastructure.Persistence;
using TransactionsIngest.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization to emit enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 1. Configure SQLite Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=transactions.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Register Repositories and Services
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IImportService, TransactionImportService>();

// 3. Register All Upstream Mock Adapters
builder.Services.AddScoped<ISourceSystemAdapter, MockUptownAdapter>();
builder.Services.AddScoped<ISourceSystemAdapter, MockDowntownAdapter>();
builder.Services.AddScoped<ISourceSystemAdapter, MockOnlineAdapter>();

// 4. Configure CORS for Angular Frontend (Default Port: 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Ensure SQLite database schema is created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ------------------------------------------------------------------
// API Endpoints
// ------------------------------------------------------------------

// POST /api/import - Initiates the transaction import operation
app.MapPost("/api/import", async (IImportService importService, CancellationToken ct) =>
{
    var result = await importService.ExecuteImportAsync(ct);
    return Results.Ok(result);
});

// GET /api/transactions - Returns imported transactions with optional query filters
app.MapGet("/api/transactions", async (
    AppDbContext db,
    string? sourceSystem,
    string? status,
    CancellationToken ct) =>
{
    var query = db.Transactions.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(sourceSystem))
    {
        query = query.Where(t => t.SourceSystem == sourceSystem);
    }

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransactionStatus>(status, true, out var parsedStatus))
    {
        query = query.Where(t => t.Status == parsedStatus);
    }

    var transactions = await query
        .OrderByDescending(t => t.TransactionTimeUtc)
        .ToListAsync(ct);

    return Results.Ok(transactions);
});

// GET /api/transactions/{sourceSystem}/{transactionId}/audit - Detailed audit logs
app.MapGet("/api/transactions/{sourceSystem}/{transactionId:int}/audit", async (
    string sourceSystem,
    int transactionId,
    AppDbContext db,
    CancellationToken ct) =>
{
    var logs = await db.AuditLogs
        .AsNoTracking()
        .Where(a => a.SourceSystem == sourceSystem && a.TransactionId == transactionId)
        .OrderByDescending(a => a.ChangedAtUtc)
        .ToListAsync(ct);

    return Results.Ok(logs);
});

app.Run();