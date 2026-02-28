# BookSmarts

Multi-company double-entry bookkeeping application for the Daisinet ecosystem.

## Overview

BookSmarts provides full-featured bookkeeping with support for:

- **Double-entry accounting** with balanced journal entries
- **Multiple companies** under a single organization with optional divisions
- **Accrual and cash basis** reporting (toggle in the header)
- **Standard US GAAP** chart of accounts templates
- **Fiscal year/period management** with open/close controls
- **Financial statements** — Balance Sheet, Income Statement, Trial Balance, Cash Flow
- **Accounts Receivable** — Invoices, customer management, payment tracking, AR aging
- **Accounts Payable** — Bills, vendor management, payment processing, AP aging
- **Banking** — Plaid bank connections, transaction import, auto-categorization rules
- **Budgeting** — Budget creation per fiscal year with budget-vs-actual reporting
- **Inter-company transactions** — Record and track transactions between companies with consolidation elimination
- **Multi-company consolidation** — Consolidated Balance Sheet and Income Statement with inter-company eliminations and optional division filtering
- **Field-level encryption** — PIN-based AES-GCM encryption for sensitive data at rest
- **Custom report builder** — Save and run custom account balance and income/expense reports
- **Audit logging** — Automatic audit trail for journal entries, invoices, and bills
- **Daisinet bot integration** — 10 bot tools for querying and recording financial data via AI
- **AI-powered insights** — On-demand AI analysis on every report page via the Daisinet inference engine
- **AI Business Coach** — Conversational chatbot with streaming responses and full financial context
- **Financial projections** — AI-generated 3-month revenue/expense/cash projections with what-if parameters
- **Cash flow forecasting** — 30/60/90-day cash position forecast using AR/AP aging and bank patterns
- **Natural language journal entries** — Describe a transaction in plain English and AI generates balanced JE lines
- **AI budget suggestions** — Auto-suggest budget amounts based on 12 months of historical actuals
- **AI bank categorization** — AI-suggested chart-of-accounts mappings for uncategorized bank transactions

## Projects

| Project | Description |
|---------|-------------|
| `BookSmarts.Core` | Domain models, enums, and interfaces |
| `BookSmarts.Data` | Cosmos DB data access layer (partial class pattern) |
| `BookSmarts.Services` | Business logic — 22 services covering all accounting domains |
| `BookSmarts.Web` | Blazor Server web application |
| `BookSmarts.Tools` | Daisinet bot tool integration (10 tools) |
| `BookSmarts.Tests` | Unit tests (284 tests) |

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

Uses Azure Cosmos DB with the `daisi` database. Containers:

- `Organizations` — Organization, Division, Company, EncryptionConfig, and BookSmartsUser documents (partitioned by `AccountId`)
- `ChartOfAccounts` — Chart of accounts entries (partitioned by `CompanyId`)
- `Journals` — Journal entries with embedded lines (partitioned by `CompanyId`)
- `Periods` — Fiscal years with embedded periods (partitioned by `CompanyId`)
- `Banking` — Bank connections, transactions, categorization rules (partitioned by `CompanyId`)
- `ArAp` — Invoices, bills, payments, customers, vendors (partitioned by `CompanyId`)
- `Budgets` — Budgets with line items (partitioned by `CompanyId`)
- `InterCompany` — Inter-company transactions (partitioned by `OrganizationId`)

### Double-Entry Bookkeeping

Every financial transaction is recorded as a journal entry with balanced debit and credit lines. The system enforces:

- At least two lines per entry
- Total debits must equal total credits
- No negative amounts
- Each line has either a debit or credit (not both)

### Accrual vs Cash Basis

Toggle between accrual and cash basis in the header. Cash basis filters journal entries by source type, excluding accrual-only entries (invoices, bills) and including only entries from cash transactions, bank imports, and payments.

### Organization Hierarchy

- **Organization** — Top-level entity for an account
- **Division** — Optional grouping layer (enabled per organization)
- **Company** — Individual bookkeeping entity with its own chart of accounts, journal, and fiscal periods

### Field-Level Encryption

Sensitive data can be encrypted at rest using PIN-based AES-GCM encryption. When enabled, organization, company, account, journal entry, invoice, bill, and other documents are encrypted before storage and decrypted on read using a session-held key derived from the user's PIN.

## User Management

BookSmarts supports multi-user access with role-based permissions, leveraging Daisinet SSO for authentication.

### Roles

| Role | Permissions |
|------|-------------|
| **Owner** | Full access — manage users, encryption, organization, companies, all features |
| **Accountant** | Write journals, invoices, bills, banking, and budgets; view reports; use AI |
| **Bookkeeper** | Write journals, invoices, bills, and banking; view reports; use AI |
| **Viewer** | View reports only |

### Auto-Provisioning

The first user to access BookSmarts for an account is automatically created as an **Owner**. Subsequent users who authenticate via Daisinet SSO but don't have a BookSmarts user record see an "Access Denied" page.

### Team Management

Owners can manage team members from **Settings > Team**:

- **Import from Daisinet** — Pull users from the Daisinet account and assign BookSmarts roles
- **Edit users** — Change roles, assign per-company access, deactivate/reactivate
- **Company access** — Non-Owner users can be restricted to specific companies. Owners with no company restrictions have access to all companies by default.
- **Safety** — The last active Owner cannot be deactivated

### Per-Company Access

- Owners with empty `CompanyIds` = access to ALL companies (default)
- Non-Owner roles must have explicit company assignments
- The company dropdown in the header is filtered by the user's company access
- API endpoints enforce company access checks in addition to account-level auth

### Services

| Service | Description |
|---------|-------------|
| `UserManagementService` | CRUD operations for BookSmarts users with encryption support |
| `PermissionService` | Static helper for role-based permission checks |
| `UserContext` | Scoped service holding the current user for the session |

## Bot Tools

BookSmarts includes 10 Daisinet bot tools for AI-driven interaction with accounting data:

| Tool ID | Description |
|---------|-------------|
| `booksmarts-balance-sheet` | Get a balance sheet as of any date |
| `booksmarts-income-statement` | Get an income statement for a date range |
| `booksmarts-trial-balance` | Get a trial balance with debit/credit columns |
| `booksmarts-chart-of-accounts` | List or search the chart of accounts |
| `booksmarts-outstanding-invoices` | List unpaid invoices (AR) |
| `booksmarts-outstanding-bills` | List unpaid bills (AP) |
| `booksmarts-aging-report` | AR or AP aging report by bucket |
| `booksmarts-create-journal-entry` | Create (and optionally post) a journal entry |
| `booksmarts-cash-flow` | Get a cash flow statement for a date range |
| `booksmarts-budget-vs-actual` | Get budget vs actual comparison report |
| `booksmarts-bank-transactions` | Get recent bank transactions with status filtering |

Tools are auto-discovered by the Daisinet host via reflection. Each tool accepts a `company-id` parameter and returns formatted markdown output.

## AI Features

BookSmarts integrates with the Daisinet inference engine to provide AI-powered financial intelligence. All AI features use the `InferenceClientFactory` registered via `AddDaisiForWeb()`.

### AI Insight Panel

Every report page (Balance Sheet, Income Statement, Cash Flow, Budget vs Actual, AR Aging, AP Aging) includes an "Analyze with AI" button. Clicking it sends the report data as structured context to the inference engine and displays a concise analysis with key trends, concerns, and actionable recommendations.

### AI Business Coach (`/ai/chat`)

A full-page conversational chatbot that streams responses in real-time. On load, it pre-fetches the company's current Balance Sheet, Income Statement, Cash Flow, and AR/AP aging data to provide informed answers. Business owners can ask questions like "How's my cash position?" or "What are my biggest expenses this quarter?" and get answers grounded in their actual data.

### Financial Projections (`/ai/projections`)

Generates a 3-month revenue, expense, net income, and cash balance projection based on the last 6 months of historical income statements, current balance sheet, and cash flow. Includes what-if sliders for revenue growth and expense change percentages.

### Cash Flow Forecast (`/ai/cash-forecast`)

Produces a 30/60/90-day cash position forecast using the current cash balance, AR/AP aging (to estimate expected inflows and outflows), and recent income trends.

### Natural Language Journal Entries

On the Journal Entry creation page, describe a transaction in plain English (e.g., "Paid $500 rent from checking") and the AI generates balanced double-entry journal lines with the correct accounts, debits, and credits.

### Budget Advisor

On the Budget Edit page, click "AI Suggest" to auto-fill empty budget cells with amounts based on the last 12 months of actual income statement data.

### Bank Categorization Suggestions

On the Bank Transactions page, click "AI Suggest" next to the account selector when categorizing a transaction. The AI recommends the most appropriate chart-of-accounts mapping based on the transaction's merchant, amount, and Plaid categories.

### Architecture

| Component | Location | Role |
|-----------|----------|------|
| `FinancialContextBuilder` | `BookSmarts.Services` | Formats report models into compact markdown for AI context |
| `BookSmartsInferenceService` | `BookSmarts.Web/Services` | Wraps `InferenceClientFactory` for single-shot, streaming, and JSON-structured inference |
| `AIInsightPanel` | `BookSmarts.Web/Components/Pages/AI` | Reusable "Analyze with AI" component for any report |
| `AIChatPage` | `BookSmarts.Web/Components/Pages/AI` | Full-page streaming chatbot |
| `AIInsightWidget` | `BookSmarts.Web/Components/Pages/Dashboard` | Dashboard health summary card |

## Custom Reports

Build and save custom report definitions that can be run on demand:

- **Account Balances** — Point-in-time balances for selected accounts
- **Income/Expense** — Period-based activity for revenue and expense accounts
- Filter by account category, sub-type, or specific accounts
- Group results by category, sub-type, or flat list
- Option to show or hide zero-balance accounts
- Print-friendly output

## Audit Logging

Automatic audit trail for key accounting actions:

- Journal entry creation, posting, voiding, and reversal
- Invoice creation and sending
- Bill creation and receiving
- Filterable by date range and entity type
- Accessible from the Audit Log page under Reports

## Roadmap

- ~~Phase 1: Core accounting — chart of accounts, journal entries, fiscal periods, trial balance~~
- ~~Phase 2: Plaid bank integration, transaction management~~
- ~~Phase 3: AR/AP with invoices, bills, and payment tracking~~
- ~~Phase 4: Balance Sheet, P&L, Cash Flow reports~~
- ~~Phase 5: Multi-company consolidation, budgeting, inter-company transactions~~
- ~~Phase 6: Daisinet bot tool integration~~
- ~~Phase 7: Custom report builder and audit logging~~
- ~~Phase 8: AI inference — report insights, business coach chatbot, projections, cash forecast, NL journal entries, budget advisor, bank categorization~~
- ~~Phase 9: User management — roles (Owner/Accountant/Bookkeeper/Viewer), per-company access, auto-provisioning, team management UI~~
