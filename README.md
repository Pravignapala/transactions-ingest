\# Retail Payments Platform — Transactions Ingest



\[!\[CI Pipeline](https://github.com/Pravignapala/transactions-ingest/actions/workflows/ci.yml/badge.svg)](https://github.com/Pravignapala/transactions-ingest/actions/workflows/ci.yml)



A high-throughput, idempotent snapshot reconciliation platform built with \*\*ASP.NET 10\*\*, \*\*Entity Framework Core\*\*, \*\*SQLite\*\*, and \*\*Angular 19\*\*. The engine ingests full snapshot dumps from upstream point-of-sale and online systems, detects field-level modifications, tracks revisions in audit history, and enforces a 24-hour revocation and maturity lifecycle.



\---



\## Architecture \& Project Structure



The solution adheres to Clean / Hexagonal architecture principles, ensuring complete decoupling between domain rules, data access, and entry points:



```text

transactions-ingest/

├── src/

│   ├── Core/               # Pure domain layer: entities, enums, DTOs, interfaces, reconciliation engine

│   ├── Infrastructure/     # EF Core SQLite DbContext, mock adapters, repositories

│   └── Api/                # ASP.NET 10 Minimal APIs, CORS, dependency injection container

├── tests/

│   └── UnitTests/          # xUnit \& FluentAssertions test suite

├── frontend/               # Angular standalone SPA with real-time import trigger \& status badges

├── .github/workflows/      # GitHub Actions CI workflow (build \& tests)

└── TransactionsIngest.slnx # .NET Solution

Core Design Decisions

Composite Primary Key (SourceSystem, TransactionId): Upstream retail sources generate overlapping local sequential identifiers (e.g., both Store Uptown and Online use 100001). The database and entities model identity as a composite pair to prevent collisions.



Strict Idempotency: Running duplicate import batches with identical incoming data produces 0 insertions, 0 updates, and 0 redundant audit records.



Field-Level Diffing: Updates calculate exact differences (Amount, ProductName, CardNumber, LocationCode, TransactionTimeUtc), persisting granular before-and-after values into AuditLogs while incrementing RevisionNumber.



24-Hour Revocation \& Finalization:



Revocation: Any record missing from a subsequent snapshot within 24 hours of its transaction timestamp transitions to Revoked with an audit trail entry.



Finalization: Records older than 24 hours become immutable (Finalized), preventing retrospective modifications.



Prerequisites

.NET 10 SDK



Node.js (LTS)



Quickstart \& Running Locally

1\. Run Unit Tests

Bash

dotnet test tests/UnitTests/UnitTests.csproj

2\. Launch the Backend API

Bash

dotnet run --project src/Api --urls "http://localhost:5000"

The API starts on http://localhost:5000 and automatically runs EnsureCreated() to generate transactions.db.



3\. Launch the Angular Frontend

In a separate terminal window:



Bash

cd frontend

npm install

npm start -- --port 4200

Navigate to http://localhost:4200 to trigger manual snapshot imports and view transaction badges.



API Endpoints

POST /api/import — Executes snapshot ingestion across all adapters, returning summary metrics (batchId, insertedCount, updatedCount, revokedCount).



GET /api/transactions — Lists all transactions, with optional filters ?sourceSystem=Store+Uptown\&status=Active.



GET /api/transactions/{sourceSystem}/{transactionId}/audit — Retrieves detailed field-level change history for a specific transaction.





\---



\### Terminal Execution Steps



1\. Open the file in Notepad from the project root:

&#x20;  ```bash

&#x20;  notepad README.md

