# CLAUDE.md - AI Assistant Guide for SGEEP.Web

## Project Overview

SGEEP (Sistema de Gestão de Estágios para Escolas Profissionais) is an internship and professional employability management system built as an ASP.NET Core 8 MVC application with a layered architecture. The UI and domain language is **Portuguese**.

## Architecture

```
SGEEP.Web/              # ASP.NET Core MVC - Controllers, Views, Services, Middleware
SGEEP.Core/             # Domain layer - Entities and Enums (no dependencies)
SGEEP.Infrastructure/   # Data layer - EF Core DbContext, Migrations (depends on Core)
SGEEP.Tests/            # Unit tests - xUnit + Moq with InMemory DB
```

### Entity Relationships

```
Curso (1) ──── (N) Aluno
Curso (1) ──── (N) Professor
Aluno (1) ──── (N) Estagio
Professor (1) ── (N) Estagio
Empresa (1) ── (N) Estagio
Estagio (1) ── (N) RegistoHoras
Estagio (1) ── (N) Relatorio
Estagio (1) ── (1) Avaliacao
IdentityUser (1) ── (0..1) Aluno | Professor | Empresa
```

### Key Enums (`SGEEP.Core/Enums/Enums.cs`)

- **TipoUtilizador**: Administrador, Professor, Aluno, Empresa
- **EstadoEstagio**: Pendente, Ativo, Concluido, Cancelado
- **EstadoRelatorio**: Rascunho, Submetido, EmRevisao, Aprovado, Rejeitado
- **EstadoHoras**: Pendente, Validado, Rejeitado

## Tech Stack

- **.NET 8.0** with nullable reference types and implicit usings
- **PostgreSQL** via Npgsql.EntityFrameworkCore.PostgreSQL
- **ASP.NET Core Identity** for authentication/authorization (4 roles)
- **Bootstrap 5.3.3** + Bootstrap Icons 1.11.3 (CDN)
- **jQuery** with unobtrusive validation
- **QuestPDF** for PDF generation, **ClosedXML** for Excel export
- **MailKit** for email
- **DinkToPdf** for HTML-to-PDF conversion

## Development Commands

```bash
# Restore and build
dotnet restore
dotnet build

# Run the web application
dotnet run --project SGEEP.Web

# Run tests
dotnet test

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project SGEEP.Infrastructure --startup-project SGEEP.Web

# Apply migrations
dotnet ef database update --project SGEEP.Infrastructure --startup-project SGEEP.Web
```

### Local URLs

- HTTP: `http://localhost:5267`
- HTTPS: `https://localhost:7110`

## Code Conventions

### Language

- All UI text, variable names, entity names, and comments are in **Portuguese**. Follow this convention for new code.

### Naming

- **PascalCase** for classes, methods, properties, and public members
- **camelCase** for local variables and parameters
- Controller names use plural Portuguese nouns (e.g., `AlunosController`, `EmpresasController`)
- ViewModels are suffixed with `ViewModel` (e.g., `EstagioViewModel`)

### Patterns

- **Controllers**: Use `[Authorize]` attributes for access control; role checks via `[Authorize(Roles = "...")]`
- **Anti-forgery**: All POST actions use `[ValidateAntiForgeryToken]`
- **Async**: All data-access methods are async (`async Task<IActionResult>`)
- **Eager loading**: Use `.Include()` for related entities — no lazy loading configured
- **Pagination**: Use `PaginatedList<T>` helper for list views
- **Audit logging**: Use `AuditoriaService` to log significant actions with user/IP tracking
- **Notifications**: Use `NotificacaoService` for user notification CRUD

### Validation

- Data annotations on ViewModels and entities
- Custom `NifValidationAttribute` for Portuguese NIF (tax ID) validation
- Password: 8+ characters, requires digit and uppercase, no special character requirement

### Security

- Security headers set in middleware (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)
- HTTPS redirection and HSTS in non-development environments
- Rate limiting on login (10 requests/minute)
- Account lockout after 5 failed attempts (15-minute lockout)
- `ForcePasswordChangeMiddleware` enforces password change via "MustChangePassword" claim

## Project Structure Details

### Controllers (SGEEP.Web/Controllers/)

| Controller | Purpose |
|---|---|
| HomeController | Landing page |
| AccountController | Auth, password change, admin password reset |
| AlunosController | Student CRUD (admin/professor view) |
| AlunoController | Student portal (self-service) |
| EmpresasController | Company CRUD |
| EmpresaPortalController | Company portal view |
| ProfessoresController | Teacher CRUD |
| EstagiosController | Internship management |
| RegistoHorasController | Hour log management |
| RelatoriosController | Report management |
| ExportController | Excel/PDF export |
| DashboardController | Dashboard views |
| NotificacoesController | Notification management |
| AuditoriaController | Audit log viewer (admin only) |

### Services (SGEEP.Web/Services/)

- **AuditoriaService** — Audit trail logging (action, entity, details, IP, user, timestamp)
- **NotificacaoService** — Notification CRUD operations

### Middleware (SGEEP.Web/Middleware/)

- **ForcePasswordChangeMiddleware** — Redirects users with "MustChangePassword" claim to password change page

### Database Seeding (SGEEP.Web/Data/SeedData.cs)

Seeds 4 roles (Administrador, Professor, Aluno, Empresa) and an admin user on startup. Eight courses are seeded via `ApplicationDbContext.OnModelCreating()`.

### Static Files

- `wwwroot/lib/` — Bootstrap, jQuery, jQuery Validation
- `wwwroot/uploads/relatorios/` — Uploaded report files

## Testing

- **Framework**: xUnit with Moq for mocking
- **Database**: InMemory provider via `TestDbHelper.CreateContext()`
- **Test file**: `SGEEP.Tests/UnitTests.cs`
- **Coverage areas**: Entity validation, business logic calculations (TotalHoras), pagination, service CRUD, ViewModel validation

Run tests with: `dotnet test`

## Configuration

- Connection strings and secrets managed via environment variables and .NET User Secrets
- User Secrets ID: `aspnet-SGEEP.Web-b05d8485-2cbb-49e1-a3eb-240add3957c5`
- See `SGEEP.Web/appsettings.json.example` for configuration template
- `appsettings.json` is gitignored — never commit connection strings or credentials

## Git Practices

- `appsettings.json` and `appsettings.*.json` are gitignored (only `.example` files tracked)
- Standard .NET `.gitignore` (bin/, obj/, .vs/, *.user excluded)
- No CI/CD pipelines configured — tests must be run locally before pushing
