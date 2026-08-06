# Company Management System

A full-stack enterprise operations platform for managing organizational hierarchy, task delegation, approvals, and internal communication — built with a **.NET 10 Clean Architecture backend** and an **Angular 22 (signals + zoneless) frontend**.

> Employees are organized in a company hierarchy tree. Managers delegate tasks to their subordinates, subordinates request approvals (task completion, holidays), and everyone gets a live activity feed and real-time chat — all backed by JWT auth, Redis caching, and SignalR.

---

## ✨ Features

- **Organizational Hierarchy Tree** — Visual, lazily-loaded company tree (CEO → HR/Manager → Programmer/Employee) with drill-down loading of children as you scroll.
- **Task Delegation** — Managers assign tasks (title, description, deadline, priority) directly to any subordinate in their branch of the tree.
- **Approval Workflows** — Two-way approval system for task completion and holiday requests, with "Needs Approval" and "Requested" views.
- **Real-Time Chat** — 1:1 messaging between organization members via SignalR, with typing indicators and lazy-loaded chat history.
- **Activity Feed** — Auto-generated audit trail (task created/completed/rejected, approval pending/approved/rejected) shown on the dashboard.
- **Employee Onboarding** — Admin-only flow to add new employees with profile photo upload (Cloudinary), position, and manager assignment.
- **Authentication & Authorization** — JWT access tokens + rotating refresh tokens (HTTP-only cookies), role-based route guards (Admin / Employee).
- **Caching Layer** — Redis-backed decorator around the organization hierarchy service to reduce database load on a frequently-read tree structure.
- **Structured Logging** — Serilog piped into Seq for centralized, queryable logs across environments.

---

## 🏗️ Architecture

The backend follows **Clean Architecture** with strict dependency inversion:

```
Company_System.Core            → Domain entities, DTOs, enums, interfaces (no infra dependencies)
Company_System.Infrastructure  → EF Core, repositories, services, Redis, background jobs
Company_System.WebApi          → Controllers, middleware, SignalR hubs, DI composition root
```

Key patterns used throughout:

- **Result / Result\<T\>** pattern instead of exceptions for expected failure paths
- **Repository + Service** layering, all behind interfaces for testability
- **Decorator pattern** — `CachedOrganizationHierarchyService` wraps the real service to add Redis caching transparently
- **Transactional filter** — wraps write operations in a database transaction at the API boundary
- **Background service** — periodic cleanup of expired refresh tokens

The frontend is a standalone-component Angular app (no NgModules) using:

- **Signals** for all reactive state (no RxJS state stores)
- **Zoneless change detection**
- Feature-based folder structure (`features/`, `core/`, `shared/`, `layout/`)
- HTTP interceptors for silent access-token refresh on 401 and automatic `Content-Type` handling
- Tailwind CSS v4 with a custom design token theme

---

## 🛠️ Tech Stack

**Backend**
- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (custom `ApplicationUser` / `ApplicationRole`)
- SignalR (real-time messaging hub)
- Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`)
- Cloudinary (image storage)
- Serilog + Seq (logging)
- xUnit (unit tests for repositories & services)

**Frontend**
- Angular 22 (standalone components, signals, zoneless)
- Tailwind CSS 4
- Reactive Forms with custom validators
- @microsoft/signalr client
- Vitest (unit tests)

**Infrastructure**
- Docker Compose (API, SQL Server, Redis, Seq, Angular/Nginx)
- HTTPS locally via mkcert-generated certificates

---

## 📁 Project Structure

```
Company_System/
├── FrontEnd/Company_System/     # Angular application
│   └── src/
│       ├── core/                # DTOs, services, guards, interceptors, enums
│       ├── features/             # dashboard, login, org-tree, add-employee
│       ├── layout/               # main layout, side bar, top nav
│       └── shared/                # reusable components & directives
├── src/
│   ├── Company_System.Core/          # Domain, DTOs, interfaces
│   ├── Company_System.Infrastructure/ # EF Core, repositories, services
│   └── Company_System.WebApi/         # Controllers, SignalR, middleware
├── tests/
│   ├── Company_System.Repositories.UnitTests/
│   └── Company_System.Services.UnitTests/
└── compose.yaml                  # Full-stack Docker orchestration
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (recommended for full-stack local run)

### Run everything with Docker Compose

1. Create a `.env` file in the project root with the required variables:

   ```env
   ASPNETCORE_ENVIRONMENT=Development
   JWT_KEY=your-secret-key
   DB_NAME=CompanySystemDb
   SA_PASSWORD=YourStrong@Passw0rd
   SeqPassword=YourSeqPassword
   CloudName=your-cloudinary-cloud-name
   ApiKey=your-cloudinary-api-key
   ApiSecret=your-cloudinary-api-secret
   ```

2. Build and start all services:

   ```bash
   docker compose up --build
   ```

3. Services will be available at:
   - API: `https://localhost:8081`
   - Angular app: `https://localhost:4000`
   - Seq logs: `http://localhost:5341`

### Run the API locally

```bash
cd src/Company_System.WebApi
dotnet restore
dotnet ef database update
dotnet run
```

### Run the Angular app locally

```bash
cd FrontEnd/Company_System
npm install
npm start
```

The dev server runs over HTTPS at `https://localhost:4200` using the certificates in `ssl/`.

---

## ✅ Testing

```bash
# Backend unit tests (repositories & services)
dotnet test

# Frontend unit tests
cd FrontEnd/Company_System
npm test
```

---

## 📌 Roadmap Ideas

- Role/permission granularity beyond Admin/Employee
- Email notifications for approvals and task deadlines
- Pagination controls in addition to infinite scroll
- E2E test coverage for critical flows (login, task delegation, approvals)

---

## 📄 License

This project is available for portfolio and educational purposes.
