MoneyTransfer System

A multi-tenant financial management system built with ASP.NET Core MVC that allows organizations to manage clients, projects, invoices, payments, discounts, and financial ledgers with full audit integrity.

--------------------------------------------------

OVERVIEW

MoneyTransfer is designed around a ledger-based architecture where:

- All financial operations are recorded as immutable transactions
- LedgerEntry is the single source of truth
- Balances are always computed, never stored
- Corrections are handled via adjustments or compensating entries (voids)

--------------------------------------------------

ARCHITECTURE

The system follows a strict layered architecture:

Domain
Application
Infrastructure
Web (MVC + Razor Views)

Key principles:

- No business logic in controllers
- Services layer is the single source of logic
- Multi-tenancy enforced via OrganizationId
- Customer isolation enforced via ClientId
- No duplication between UI and API

--------------------------------------------------

CORE FEATURES

Organization Portal

- Manage clients and projects
- Create invoices
- Record payments (project-level and client allocation)
- Apply discounts
- Export statements (PDF and Excel)
- API key management
- Adjustment entries (corrections)
- Void transactions (via reversal entries)

Customer Portal

- View transactions
- Running balance
- Filters and exports

--------------------------------------------------

LEDGER MODEL

All financial data flows through LedgerEntry.

Transaction types:

- Invoice (positive)
- Payment (negative)
- Discount (negative)
- Adjustment (positive or negative)

Key rules:

- Transactions are immutable
- No edits or deletes allowed
- Corrections are done via:
  - Adjustment entries
  - Void (compensating entries)

--------------------------------------------------

API (AUTOMATION LAYER)

Authentication

API uses an API key per organization.

Header:
X-API-KEY: {your-api-key}

- Keys are stored hashed using HMAC-SHA256
- Raw key is shown only once at creation
- Revoked keys cannot be used

--------------------------------------------------

API ENDPOINTS

Create Invoice
POST /api/v1/invoices

Example payload:
{
  "projectId": "GUID",
  "amount": 100,
  "date": "2026-04-01",
  "description": "Invoice description"
}

Project Payment
POST /api/v1/payments/project

Example payload:
{
  "projectId": "GUID",
  "amount": 50,
  "method": "Cash"
}

Client Payment Allocation
POST /api/v1/payments/client

Example payload:
{
  "clientId": "GUID",
  "totalAmount": 100,
  "allocations": [
    { "projectId": "GUID", "amount": 60 },
    { "projectId": "GUID", "amount": 40 }
  ]
}

--------------------------------------------------

API KEY MANAGEMENT

Available in Organization Portal:

- Generate API key
- View key prefix
- Revoke key

Rules:

- Only one active key per organization
- Raw key is never stored
- Raw key is displayed once only

--------------------------------------------------

SETUP INSTRUCTIONS

Prerequisites:

- .NET 8 SDK
- SQL Server (Express or LocalDB)
- Visual Studio or VS Code

1. Clone Repository

git clone https://github.com/YOUR_USERNAME/MoneyTransfer.git
cd MoneyTransfer

2. Configure Database

Update connection string in:
src/MoneyTransfer.Web/appsettings.json

3. Run Migrations

dotnet ef database update --project src/MoneyTransfer.Infrastructure --startup-project src/MoneyTransfer.Web

4. Run Application

dotnet run --project src/MoneyTransfer.Web

Navigate to:
https://localhost:xxxx

--------------------------------------------------

HOW TO USE

1. Register Organization
- Create an organization account from the landing page

2. Login (Organization User)
- Access Organization Portal

3. Create Client
- Add a client

4. Create Project
- Add project under client

5. Start Transactions
- Create invoices
- Record payments
- Apply discounts

--------------------------------------------------

CORRECTIONS

Adjustment

- Manual correction
- Can be positive or negative
- Requires reason
- Admin-only

Void

- Reverses an existing transaction
- Creates a compensating entry
- Original entry remains unchanged

--------------------------------------------------

EXPORTS

- PDF statement
- Excel statement

Includes:

- Transaction history
- Running balance
- Proper formatting

--------------------------------------------------

DESIGN DECISIONS

- Ledger-first architecture
- No balance fields stored
- Structured references:
  - InvoiceNumber
  - PaymentReference
  - DiscountReference
- Notes field contains only descriptive text

--------------------------------------------------

NOTES

- Test data may be reset during development
- Single API key per organization (no rotation system)
- Rate limiting not implemented (out of scope)

--------------------------------------------------

FINAL STATUS

The system includes:

- Full financial workflow
- Secure API layer
- Immutable ledger model
- Correction mechanisms
- UI and UX improvements

--------------------------------------------------

FUTURE IMPROVEMENTS

- Multi-key API support
- Rate limiting
- Full audit UI
- Currency support
- Advanced reporting

--------------------------------------------------

AUTHOR

Developed as a professional training project using ASP.NET Core and EF Core.