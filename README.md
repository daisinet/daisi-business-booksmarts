# BookSmarts

Multi-company double-entry bookkeeping application for the Daisinet ecosystem.

## Overview

BookSmarts provides full-featured bookkeeping with support for:

- **Double-entry accounting** with balanced journal entries
- **Multiple companies** under a single organization with optional divisions
- **Accrual and cash basis** reporting (toggle in the header)
- **Standard US GAAP** chart of accounts templates
- **Fiscal year/period management** with open/close controls
- **Trial balance** reporting with real-time calculations

## Projects

| Project | Description |
|---------|-------------|
| `BookSmarts.Core` | Domain models, enums, and interfaces |
| `BookSmarts.Data` | Cosmos DB data access layer (partial class pattern) |
| `BookSmarts.Services` | Business logic — accounting, periods, chart of accounts |
| `BookSmarts.Web` | Blazor Server web application |
| `BookSmarts.Tools` | Daisinet tool integration (future) |
| `BookSmarts.Tests` | Unit tests |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure Cosmos DB account (or emulator)

### Configuration

Set the Cosmos DB connection string in user secrets:

```bash
cd BookSmarts.Web
dotnet user-secrets set "Cosmo:ConnectionString" "AccountEndpoint=https://...;AccountKey=..."
```

### Running

```bash
cd BookSmarts.Web
dotnet run --launch-profile https
```

The app runs at `https://localhost:5201`.

### Running Tests

```bash
dotnet test BookSmarts.Tests
```

## Architecture

### Data Layer

Uses Azure Cosmos DB with the `daisi` database. All containers are prefixed with `BS_`:

- `BS_Organizations` — Organization, Division, and Company documents (partitioned by `AccountId`)
- `BS_ChartOfAccounts` — Chart of accounts entries (partitioned by `CompanyId`)
- `BS_Journals` — Journal entries with embedded lines (partitioned by `CompanyId`)
- `BS_Periods` — Fiscal years with embedded periods (partitioned by `CompanyId`)

### Double-Entry Bookkeeping

Every financial transaction is recorded as a journal entry with balanced debit and credit lines. The system enforces:

- At least two lines per entry
- Total debits must equal total credits
- No negative amounts
- Each line has either a debit or credit (not both)

### Accrual vs Cash Basis

Toggle between accrual and cash basis in the header. Cash basis filters journal entries by source type, excluding accrual-only entries (invoices, bills) and including only entries from cash transactions, bank imports, and payments.

## Roadmap

- **Phase 2**: Plaid bank integration, transaction management
- **Phase 3**: AR/AP with invoices, bills, and payment tracking
- **Phase 4**: Balance Sheet, P&L, Cash Flow reports with PDF/Excel export
- **Phase 5**: Multi-company consolidation and budgeting
- **Phase 6**: Daisinet bot tool integration
- **Phase 7**: Custom report builder and audit logging
