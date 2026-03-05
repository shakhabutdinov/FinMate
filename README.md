# FinMate API — ASP.NET 9 Backend

REST API backend for the FinMate personal finance management application. Built with ASP.NET 9, Entity Framework Core, and PostgreSQL.

## Tech Stack

- **Runtime**: .NET 9
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9 (Npgsql provider)
- **Database**: PostgreSQL
- **Authentication**: JWT Bearer tokens + Google OAuth
- **Payment**: Google Pay SDK integration
- **Password Hashing**: BCrypt

## Project Structure

```
aspnet/
├── Controllers/          # API endpoints
│   ├── AuthController    # POST /api/auth/login, register, google
│   ├── DashboardController # GET /api/dashboard
│   ├── StocksController  # GET/POST/DELETE /api/stocks
│   ├── CryptoController  # GET/POST/DELETE /api/crypto
│   ├── PfmController     # /api/pfm/overview, transactions, goals
│   ├── AiController      # /api/ai/history, message, quick-questions
│   └── PaymentController # POST /api/payment, GET /api/payment/config
├── Data/
│   └── FinMateDbContext   # EF Core DbContext with seed data
├── DTOs/                  # Request/response data transfer objects
├── Models/                # Entity models
│   ├── User, Asset, StockHolding, CryptoHolding
│   ├── Transaction, FinancialGoal, ChatMessage
│   └── TrendingItem
├── Repositories/          # Repository pattern (separate per entity)
│   ├── Interfaces/        # Repository contracts
│   └── *Repository.cs     # Implementations
├── Services/              # Business logic layer
│   ├── AuthService        # JWT generation, Google OAuth
│   ├── DashboardService   # Portfolio overview
│   ├── StockService       # Stock holdings management
│   ├── CryptoService      # Crypto holdings management
│   ├── PfmService         # Transactions, goals, cashflow
│   ├── AiService          # AI chat responses
│   └── PaymentService     # Google Pay integration
├── appsettings.json       # Configuration (placeholder keys)
└── Program.cs             # App startup and DI registration
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (running on localhost:5432)

## Configuration

Update `appsettings.json` with your real values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=finmate;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY_MINIMUM_32_CHARACTERS",
    "Issuer": "FinMate",
    "Audience": "FinMateApp"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "GooglePay": {
    "MerchantId": "YOUR_MERCHANT_ID",
    "MerchantName": "FinMate",
    "Environment": "TEST"
  }
}
```

## Entity Framework

### Install dependencies

Install the EF Core design-time tools and the PostgreSQL provider (versions match .NET 9):

```bash
# From the aspnet project directory
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4
```

Or add/restore via the project file and then run:

```bash
dotnet restore
```

### Create the first migration

Ensure your `DbContext` is in the startup project (e.g. `Data/FinMateDbContext.cs`) and the connection string in `appsettings.json` is valid, then:

```bash
# Create a migration named InitialCreate (or any name you prefer)
dotnet ef migrations add InitialCreate --project . --startup-project .
```

If the EF tools are not installed globally, install them once:

```bash
dotnet tool install --global dotnet-ef
```

### Update the database

Apply pending migrations to the database:

```bash
dotnet ef database update --project . --startup-project .
```

To roll back to a specific migration (e.g. the previous one):

```bash
dotnet ef database update PreviousMigrationName --project . --startup-project .
```

## Getting Started

```bash
# Restore dependencies
dotnet restore

# Create the database (auto-created on first run with seed data)
dotnet run

# Or run in development mode
dotnet watch run
```

The API starts at `http://localhost:5100`.

## Seed Data

The database is seeded with a demo user on first run:

- **Email**: `john.doe@example.com`
- **Password**: `Test123!`

Includes sample assets, stock holdings (AAPL, TSLA, NVDA), crypto holdings (BTC, ETH, SOL), transactions, and trending market data.

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login with email/password |
| POST | `/api/auth/google` | No | Login with Google OAuth |
| GET | `/api/dashboard` | Yes | Dashboard overview |
| GET | `/api/stocks` | Yes | Stock portfolio |
| POST | `/api/stocks` | Yes | Add stock holding |
| DELETE | `/api/stocks/{id}` | Yes | Remove stock holding |
| GET | `/api/crypto` | Yes | Crypto portfolio |
| POST | `/api/crypto` | Yes | Add crypto holding |
| DELETE | `/api/crypto/{id}` | Yes | Remove crypto holding |
| GET | `/api/pfm/overview` | Yes | Financial overview |
| GET | `/api/pfm/transactions` | Yes | Transaction list |
| POST | `/api/pfm/transactions` | Yes | Create transaction |
| GET | `/api/pfm/goals` | Yes | Financial goals |
| POST | `/api/pfm/goals` | Yes | Create goal |
| GET | `/api/ai/history` | Yes | Chat history |
| POST | `/api/ai/message` | Yes | Send message to AI |
| GET | `/api/ai/quick-questions` | Yes | Suggested questions |
| DELETE | `/api/ai/history` | Yes | Clear chat history |
| POST | `/api/payment` | Yes | Create payment |
| GET | `/api/payment/config` | Yes | Google Pay config |

## CORS

Configured to allow requests from the Angular frontend at `http://localhost:4200`.
