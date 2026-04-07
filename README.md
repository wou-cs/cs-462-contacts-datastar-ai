# Contact List — Datastar + ASP.NET Core Demo

A contact management app built with ASP.NET Core MVC and [Datastar](https://data-star.dev/), a lightweight reactive framework that uses **Server-Sent Events (SSE)** to push HTML and signal updates from the server to the browser — no client-side JavaScript framework required.

## Features

- **Add contacts** with name, email, phone, category, and notes
- **Delete contacts** with immediate table refresh
- **Live search** across all fields (debounced 300ms)
- **Inline validation** as you type, powered by server-side DataAnnotations (debounced 500ms)
- **SSE-driven UI** — all DOM updates are pushed from the server, no full page reloads

## Tech Stack

| Layer        | Technology |
|--------------|------------|
| Backend      | ASP.NET Core 8.0 MVC (C#) |
| Reactivity   | [Datastar](https://data-star.dev/) 1.2.0 via SSE |
| Database     | SQL Server (via Docker) + Entity Framework Core |
| Styling      | Bootstrap 5.3 |
| Testing      | xUnit |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later)
- [Docker](https://www.docker.com/) (for SQL Server)

## Getting Started

### 1. Start SQL Server in Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/mssql-server:2022-latest
```

### 2. Configure the connection string

Create an `appsettings.json` in the project root (this file is git-ignored):

```json
{
  "ConnectionStrings": {
    "ContactDb": "Server=localhost,1433;Database=ContactList;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
  }
}
```

### 3. Apply the database migration

```bash
dotnet ef database update
```

This creates the `ContactList` database and seeds it with three sample contacts.

### 4. Run the app

```bash
dotnet run
```

Then open the URL shown in the terminal (typically `http://localhost:5000`).

## Running Tests

```bash
dotnet test
```

Tests cover the repository layer and contact validation logic.

## Project Structure

```
├── Controllers/
│   └── ContactController.cs        # MVC controller with SSE endpoints
├── Models/
│   ├── Contact.cs                   # Entity with DataAnnotations
│   ├── ContactDbContext.cs          # EF Core DbContext with seed data
│   ├── ContactValidator.cs          # Validation utility
│   ├── DbContactRepository.cs      # EF Core repository implementation
│   ├── IContactRepository.cs       # Repository interface
│   └── StaticContactRepository.cs  # In-memory implementation (for reference)
├── Views/
│   ├── Contact/
│   │   ├── Index.cshtml             # Main page with Datastar attributes
│   │   └── _ContactTable.cshtml     # Partial view for the contact table
│   └── Shared/
│       └── _Layout.cshtml           # Layout with Bootstrap + Datastar CDN
├── Migrations/                      # EF Core migrations
├── Program.cs                       # DI setup, routing, EF Core registration
└── ContactList-Datastar.Tests/      # xUnit test project
```

## How Datastar Works in This App

Datastar replaces the typical SPA client-side rendering model with a server-driven approach:

1. **Signals** (`data-signals`) declare reactive state in the browser (e.g., form values, visibility toggles)
2. **Bindings** (`data-bind`) two-way bind inputs to signals
3. **Actions** (`data-on`) trigger SSE requests to the server (`@get`, `@post`, `@delete`)
4. The server responds with SSE events that either **patch HTML** into the DOM or **update signal values**
5. Datastar automatically applies these patches — no manual DOM manipulation needed

This means validation errors, table refreshes, and form resets all flow through the same SSE mechanism, keeping the client thin and the server in control.
