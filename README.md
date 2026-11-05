# Retail Payments Platform — Transactions Ingest

[![CI Pipeline](https://github.com/Pravignapala/transactions-ingest/actions/workflows/ci.yml/badge.svg)](https://github.com/Pravignapala/transactions-ingest/actions/workflows/ci.yml)

A high-throughput, idempotent snapshot reconciliation platform built with **ASP.NET 10**, **Entity Framework Core**, **SQLite**, and **Angular 19**. The engine ingests full snapshot dumps from upstream point-of-sale and online systems, detects field-level modifications, tracks revisions in audit history, and enforces a 24-hour revocation and maturity lifecycle.

---

## Architecture & Project Structure

The solution adheres to Clean / Hexagonal architecture principles, ensuring complete decoupling between domain rules, data access, and entry points:

```text
transactions-ingest/
├── src/
│   ├── Core/               # Pure domain layer: entities, enums, DTOs, interfaces, reconciliation engine
│   ├── Infrastructure/     # EF Core SQLite DbContext, mock adapters, repositories
│   └── Api/                # ASP.NET 10 Minimal APIs, CORS, dependency injection container
├── tests/
│   └── UnitTests/          # xUnit & FluentAssertions test suite
├── frontend/               # Angular standalone SPA with real-time import trigger & status badges
├── .github/workflows/      # GitHub Actions CI workflow (build & tests)
└── TransactionsIngest.slnx # .NET Solution
