# Order Management System (OMS)

A full-stack Order Management System built with ASP.NET Core, Blazor Server, and MySQL.

## 🏗️ Architecture

```
OrderManagementSystem/
├── src/
│   ├── OMS.Domain          # Entities, enums, interfaces
│   ├── OMS.Application     # DTOs, service interfaces
│   ├── OMS.Infrastructure  # EF Core, repositories, services
│   ├── OMS.API             # REST API with Swagger
│   └── OMS.Web             # Blazor Server frontend
└── tests/
    └── OMS.Tests            # Unit tests (xUnit)
```

**Clean Architecture**: Controllers → Services → Repositories → EF Core

## 🚀 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- MySQL Server (v8.0+) or MariaDB

## ⚡ Quick Start

### 1. Clone & Restore
```bash
git clone <repo-url>
cd OrderManagementSystem
dotnet restore
```

### 2. Run the API (with Swagger)
```bash
dotnet run --project src/OMS.API
```
Open **https://localhost:5001/swagger** to explore the API.

### 3. Run the Blazor Frontend
```bash
dotnet run --project src/OMS.Web
```
Open **https://localhost:5002** (or the URL shown in console).

### 4. Run Tests
```bash
dotnet test
```

> **Note**: The database is auto-created with seed data on first run (using `EnsureCreated()`).

## 🔐 Authentication

### Default Users (Seed Data)
| Email | Password | Role |
|-------|----------|------|
| admin@oms.com | Admin@123 | Admin |
| user@oms.com | User@123 | User |

### API Authentication
1. Call `POST /api/auth/login` with email/password to get a JWT token
2. Include the token as `Authorization: Bearer <token>` in subsequent requests
3. Admin endpoints (Delete, Fulfill) require Admin role

### Google OAuth
Update `appsettings.Development.json` with your Google Cloud OAuth 2.0 credentials:
```json
"Google": {
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET"
}
```

## 📦 Features

- **Orders**: Create, edit, delete, fulfill with automatic inventory deduction
- **Products**: CRUD with SKU tracking, stock quantity management, low-stock alerts
- **Customers**: CRUD with email uniqueness, order history
- **Dashboard**: Real-time stats (revenue, order counts, low-stock items)
- **Pagination/Search/Sort**: On all list endpoints and pages
- **Audit Logging**: All order changes are tracked
- **Swagger UI**: Full API documentation with JWT auth support

## 🗄️ Database Schema

| Table | Description |
|-------|-------------|
| Users | OAuth-linked user accounts with roles |
| Customers | Customer profiles (name, email, address, phone) |
| Products | Product catalog with SKU and stock |
| Orders | Order header with status tracking |
| OrderItems | Order line items linking to products |
| AuditLogs | Change tracking for orders |

## 🛠️ Configuration

All configuration is in `appsettings.Development.json`:

- **ConnectionStrings:DefaultConnection** — MySQL connection string (Server=...;Database=...;User=...;Password=...)
- **Jwt:Key** — JWT signing key (min 32 characters)
- **Jwt:Issuer / Audience** — Token issuer/audience
- **Google:ClientId / ClientSecret** — Google OAuth credentials
