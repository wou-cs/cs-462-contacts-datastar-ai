# AGENTS.md

## Purpose

This repository is a teaching demo for a server-driven contacts app built with ASP.NET Core MVC, Razor, Entity Framework Core, and Datastar.

The main architectural lesson is that the browser stays thin:
- Razor serves the initial page.
- Datastar sends browser signal state to the server.
- Controller actions respond by patching HTML or signal values back over SSE.
- No custom client-side JavaScript framework is used.

## High-Level Architecture

### Runtime wiring

- `Program.cs` registers MVC, Datastar, EF Core SQL Server, and `IContactRepository -> DbContactRepository`.
- Default route is `ContactController.Index`.

### Domain model

- `Models/Contact.cs` is both the EF Core entity and the validation model.
- Validation uses DataAnnotations:
  - `Name` is required
  - `Category` is required
  - `Email` must be a valid email when present
  - `Phone` must be a valid phone number when present
- `Models/ContactValidator.cs` runs annotation-based validation manually and returns a property-name-to-error-message dictionary.

### Persistence

- `Models/ContactDbContext.cs` defines the `Contacts` table and seeds three sample contacts.
- `Models/DbContactRepository.cs` is the active repository used by DI.
- `Models/StaticContactRepository.cs` is an in-memory reference implementation used by tests and useful for teaching/comparison.

### UI pattern

- `Views/Contact/Index.cshtml` is the core teaching file.
- It declares Datastar signals in markup and binds form fields directly to those signals.
- User actions trigger Datastar `@get`, `@post`, and `@delete` requests.
- The server replies by:
  - patching HTML into `#contact-list`
  - patching validation/form state signals

- `Views/Contact/_ContactTable.cshtml` is rendered server-side and patched into the DOM as HTML.
- `Views/Shared/_Layout.cshtml` loads Bootstrap and the Datastar browser script.

## Request Flow

### Initial load

1. `GET /Contact/Index` returns the normal Razor page.
2. `#contact-list` runs `data-init="@@get('/Contact/List')"`.
3. `ContactController.List()` renders `_ContactTable` and patches it into the page.

### Search

1. The search input updates the `query` signal.
2. Debounced input triggers `GET /Contact/Search`.
3. `ContactController.Search()` reads `SearchSignals` from Datastar, filters contacts, renders `_ContactTable`, and patches the updated HTML.

### Inline validation

1. Form fields update Datastar signals such as `name`, `email`, and `phone`.
2. Debounced keydown or change events trigger `POST /Contact/Validate`.
3. `ContactController.Validate()` rebuilds a `Contact` from current signals.
4. Validation errors are mapped to `nameError`, `emailError`, `phoneError`, and `categoryError`.
5. Datastar patches those signal values back into the page.

### Create

1. Save triggers `POST /Contact/Create`.
2. The controller rebuilds the `Contact`, validates it, and stops early if errors exist.
3. On success, the repository persists the contact.
4. The controller patches the refreshed table HTML.
5. The controller then resets form signals and hides the form.

### Delete

1. Delete triggers `DELETE /Contact/Delete/{id}`.
2. The controller removes the contact.
3. The controller patches the refreshed table HTML.

## Important Implementation Details

- The controller manually renders the `_ContactTable` partial to a string before calling Datastar `PatchElementsAsync`.
- That render-to-string step is the key server-side integration point between Razor and Datastar.
- `SearchSignals` and `ContactSignals` are small DTOs used only for Datastar signal deserialization.
- The repo uses synchronous repository methods even though controller actions are async because the async behavior is around Datastar I/O, not EF calls.

## Tests

- `ContactList-Datastar.Tests/ContactValidationTests.cs` covers validation rules.
- `ContactList-Datastar.Tests/StaticContactRepositoryTests.cs` covers repository semantics for the in-memory implementation.
- Reqnroll + Selenium headless Chrome acceptance tests live alongside the unit tests in the same test project.
- Browser-driven BDD tests launch the real app process against SQLite instead of SQL Server so acceptance tests do not depend on Docker.
- Current test coverage does not exercise:
  - much controller behavior beyond the BDD smoke path
  - broad Datastar interaction coverage
  - EF Core repository behavior in isolation
  - many rendered HTML edge cases

## Known Mismatches And Rough Edges

- The README says Datastar `1.2.0`, and the server package is `1.2.0`, but `_Layout.cshtml` loads browser script `1.0.0-RC.8` from CDN.
- The README says search works across all fields, but `ContactController.Search()` does not search `Notes`.
- Categories are duplicated in `Views/Contact/Index.cshtml` instead of being sourced from `Contact.GetCategories()` or the repository.
- The `StaticContactRepository` exists mainly as a teaching/testing artifact; runtime DI uses `DbContactRepository`.
- There are no tests protecting the controller's Datastar patch behavior, so UI regressions would currently be caught manually.

## Guidance For Future Changes

- Preserve the server-driven model unless the point of the exercise changes.
- If adding fields to `Contact`, update all of:
  - the entity
  - validation expectations
  - Datastar signal DTOs
  - form signals in `Index.cshtml`
  - create/validate flows in the controller
  - `_ContactTable.cshtml` if the field should be displayed
  - seed data and migration as needed
- If changing search semantics, keep README claims aligned with `ContactController.Search()`.
- If changing Datastar versions, keep the NuGet package and browser script version aligned.
- If improving confidence, add controller/integration tests before broad UI refactors.

## Local Workflow Notes

- The local helper `gpush` is a thin wrapper around:
  - `git add .`
  - `git commit -m "<message>"`
  - `git push`
- Because of that behavior, `gpush` should be used when there are uncommitted changes to stage and commit.
- `gpush` will fail on a clean working tree that already has a local commit waiting to be pushed, because the `git commit` step exits without creating a new commit.
- If a commit has already been created manually, use plain `git push` instead of `gpush`.
