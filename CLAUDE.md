# CLAUDE.md

## Project Overview

CS-462 teaching demo: a contacts CRUD app using ASP.NET Core 8.0 MVC + Datastar (server-driven reactive UI over SSE). No client-side JS framework — the browser stays thin, server pushes HTML patches and signal updates.

## Quick Reference

```bash
dotnet build                    # Compile
dotnet run                      # Run dev server (http://localhost:5000)
dotnet test                     # Run all tests (unit + acceptance)
dotnet ef database update       # Apply EF Core migrations
dotnet ef migrations add <name> # Create new migration
```

**SQLite quick start** (no Docker needed):
```bash
dotnet run --DatabaseProvider Sqlite --ConnectionStrings:ContactDb "Data Source=contacts.db"
```

**SQL Server** (production): requires Docker container on port 1433. See README.md for setup.

## Architecture

- **Pattern**: Server-driven MVC. Razor renders initial HTML; Datastar attributes declare reactive state; user actions trigger SSE requests (`@get`, `@post`, `@delete`); controller renders partial views to strings and patches them back via SSE.
- **Layers**: Views (Razor + Datastar attributes) → ContactController (SSE orchestration) → IContactRepository (CRUD interface) → DbContactRepository (EF Core) / StaticContactRepository (in-memory for tests)
- **DI**: `IContactRepository → DbContactRepository` (scoped), `ContactDbContext` (scoped), `IDatastarService` (from Datastar NuGet)
- **Validation**: DataAnnotations on `Contact` model, run manually via `ContactValidator.GetErrors()`, errors patched to client as signal updates

## Key Files

| File | Role |
|------|------|
| `Program.cs` | DI, routing, DB config, auto-migration |
| `Controllers/ContactController.cs` | 6 SSE endpoints (Index, List, Search, Validate, Create, Delete) |
| `Models/Contact.cs` | Entity + DataAnnotations + `GetCategories()` |
| `Models/IContactRepository.cs` | Repository interface (GetAll, GetById, Add, Remove, GetCategories) |
| `Models/DbContactRepository.cs` | EF Core implementation |
| `Models/ContactDbContext.cs` | DbContext with seed data (Alice, Bob, Carol) |
| `Views/Contact/Index.cshtml` | Main page: signals, search, form, contact-list container |
| `Views/Contact/_ContactTable.cshtml` | Partial: contact table, always wrapped in `<div id="contact-list">` |

## Conventions

- **Namespaces**: `ContactList`, `ContactList.Models`, `ContactList.Controllers`
- **Style**: File-scoped namespaces, nullable reference types enabled, implicit usings, C# 11+
- **Naming**: PascalCase for types/methods/properties, camelCase for locals, `_camelCase` for private fields
- **Frontend**: All reactivity via Datastar HTML attributes — no custom JS. Bootstrap 5.3 for styling.
- **Repository**: Sync methods, immediate `SaveChanges()` in Add/Remove

## Testing

Three layers:
1. **Unit tests** — `ContactValidationTests.cs` (DataAnnotations), `StaticContactRepositoryTests.cs` (in-memory repo)
2. **BDD acceptance tests** — Reqnroll (Gherkin) + Selenium headless Chrome
   - `TestAppHost.cs` spins up the real app against a temp SQLite DB
   - `TestHooks.cs` manages WebDriver and app lifecycle
   - Feature files in `ContactList-Datastar.Tests/Features/`

## Known Issues

- Datastar version mismatch: NuGet 1.2.0 vs CDN script 1.0.0-RC.8 in `_Layout.cshtml`
- Search doesn't cover the Notes field
- Categories are hardcoded in `Contact.GetCategories()`
- `appsettings.json` is git-ignored — must be created manually for SQL Server

## Git Workflow

Use `gpush "commit message"` to add, commit, and push in one step.
