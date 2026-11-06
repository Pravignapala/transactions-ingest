\# Submission Notes — Retail Payments Platform (Transactions Ingest)



\## 1. Repository \& Deliverables

\- \*\*GitHub Repository\*\*: https://github.com/Pravignapala/transactions-ingest

\- \*\*Branch\*\*: `main`

\- \*\*CI Pipeline\*\*: Automated build and unit test execution via GitHub Actions (`.github/workflows/ci.yml`)



\---



\## 2. Time Tracking



\- \*\*Estimated Time\*\*: 6.0 hours

\- \*\*Actual Time Spent\*\*: 5.5 hours



\### Breakdown of Hours

\- \*\*Scaffolding \& Architecture (1.0 hr)\*\*: Clean Architecture setup, entity modeling, composite key configuration in EF Core SQLite, and dependency injection wiring.

\- \*\*Reconciliation Engine (1.5 hrs)\*\*: Idempotent snapshot diffing, field-level audit tracking, and automated 24-hour revocation and maturity lifecycle enforcement.

\- \*\*Automated Unit Testing (1.0 hr)\*\*: xUnit test suite with FluentAssertions covering composite keys, idempotency, updates, revocations, and immutability.

\- \*\*Angular 19 Frontend (1.0 hr)\*\*: Standalone UI with transaction status badges, manual import triggers, and audit log inspection.

\- \*\*CI/CD \& Documentation (1.0 hr)\*\*: GitHub Actions workflow configuration, SQLite persistence validation, and comprehensive README documentation.



\---



\## 3. Highlights \& Key Coding Practices



\- \*\*Decoupled Reconciliation Engine (`IImportService`)\*\*: The core ingestion and reconciliation engine is isolated from HTTP and controller layers. An external scheduler, background worker, or hosted service can trigger `ExecuteImportAsync()` directly without code changes.

\- \*\*Composite Primary Key Architecture\*\*: Modeled identity strictly as `(SourceSystem, TransactionId)` across domain entities and EF Core to eliminate identifier collisions across Store Uptown, Store Downtown, and Online feeds.

\- \*\*Zero-Op Idempotency\*\*: Running identical snapshot imports consecutively produces 0 inserts, 0 updates, and 0 redundant audit records.

\- \*\*Granular Field-Level Audit Trail\*\*: Detects precise attribute differences (`Amount`, `ProductName`, `CardNumber`, `LocationCode`) and records before-and-after audit logs while incrementing `RevisionNumber`.

\- \*\*24-Hour Lifecycle Management\*\*: Unlisted records within the 24-hour cutoff transition to `Revoked`, while mature records older than 24 hours lock into a `Finalized` immutable state.

\- \*\*Continuous Integration Pipeline\*\*: Automated GitHub Actions workflow compiling the .NET 10 backend, executing tests, and verifying Angular production build on every push.



\---



\## 4. Challenges Encountered \& Resolutions (STAR Format)



\### Handling Duplicate Records on Repeated Imports

\- \*\*Situation\*\*: The import process must run repeatedly without creating duplicates or failing on already-ingested data.

\- \*\*Task\*\*: Ensure mature records from previous runs are recognized and safely bypassed on subsequent imports.

\- \*\*Action\*\*: Initial repository queries filtered out finalized records, leading the engine to treat existing rows as new and attempt an `INSERT`, triggering an SQLite composite unique constraint collision. Refactored queries to load all system records for presence checking and explicitly bypassed updates on immutable rows.

\- \*\*Result\*\*: Achieved zero-op idempotency—subsequent imports with identical data run with zero duplicate entries and zero constraint errors.



\### Setting Up the Frontend Display

\- \*\*Situation\*\*: A clean web interface was needed to display imported transactions and trigger manual runs.

\- \*\*Task\*\*: Scaffold a standalone Angular frontend alongside the .NET solution without dependency conflicts.

\- \*\*Action\*\*: Local CLI package version mismatches initially blocked workspace setup. Updated package tooling and pinned the build directly to `@angular/cli@19` standalone components.

\- \*\*Result\*\*: Clean standalone Angular application connected to the ASP.NET 10 Minimal API with real-time status badges.



\### Automated Cloud Testing File Discovery

\- \*\*Situation\*\*: GitHub Actions needed to build and verify tests on every repository push.

\- \*\*Task\*\*: Ensure the Linux runner reliably discovers and builds project dependencies.

\- \*\*Action\*\*: The workflow initially looked for a traditional `.sln` file and failed with `MSB1009` because .NET 10 generated the modern `.slnx` format. Updated the CI steps to target `tests/UnitTests/UnitTests.csproj` directly, transitively compiling and testing all dependent projects.

\- \*\*Result\*\*: Reliable, automated test runs with a green status badge on GitHub.



\### Keeping Audit Logs Accurate (Preventing False-Positive Diffs)

\- \*\*Situation\*\*: Upstream systems send snapshots with varying decimal formatting and timestamp strings.

\- \*\*Task\*\*: Prevent false-positive change detections from cluttering the audit history.

\- \*\*Action\*\*: Enforced numeric equality on `decimal(18,2)` values and standardized all incoming date strings to strict UTC before evaluating diffs.

\- \*\*Result\*\*: Audit entries are created only for genuine business data modifications.



\---



\## 5. AI Usage Disclosure



AI tooling was utilized strictly as an accelerator for boilerplate syntax and scaffolding checks. All architectural decisions (Hexagonal architecture, composite key schema, state transition lifecycle, and idempotency logic), debugging of runtime SQLite constraint conflicts, and CI pipeline tuning were engineered, tested, and verified independently.

