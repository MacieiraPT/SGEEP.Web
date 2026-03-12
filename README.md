# SGEEP — Sistema de Gestão de Estágios para Escolas Profissionais

Aplicação web para gestão de estágios em escolas profissionais, desenvolvida em ASP.NET Core 8 MVC com arquitetura em camadas.

## Funcionalidades

- **Gestão de estágios** — criação, acompanhamento e conclusão de estágios por alunos, professores e empresas
- **Registo de horas** — submissão e validação de horas de estágio
- **Relatórios** — submissão, revisão e aprovação de relatórios de estágio
- **Avaliações** — avaliação dos estágios pelos orientadores
- **Notificações** — sistema interno de notificações para os utilizadores
- **Auditoria** — registo de ações significativas no sistema
- **Exportação** — exportação de dados para Excel e PDF
- **Dashboard** — painéis de controlo por perfil de utilizador

## Perfis de Utilizador

| Perfil | Descrição |
|---|---|
| Administrador | Gestão total do sistema, utilizadores e cursos |
| Professor | Orientação de estágios, validação de horas e relatórios |
| Aluno | Submissão de candidaturas, registo de horas e relatórios |
| Empresa | Publicação de vagas e acompanhamento de estagiários |

## Arquitetura

```
SGEEP.Web/            # ASP.NET Core MVC — Controllers, Views, Services, Middleware
SGEEP.Core/           # Camada de domínio — Entidades e Enums (sem dependências)
SGEEP.Infrastructure/ # Camada de dados — EF Core DbContext e Migrations
SGEEP.Tests/          # Testes unitários — xUnit + Moq com InMemory DB
```

## Tecnologias

- **.NET 8.0** com nullable reference types e implicit usings
- **PostgreSQL** via Npgsql.EntityFrameworkCore.PostgreSQL
- **ASP.NET Core Identity** — autenticação e autorização com 4 papéis
- **Bootstrap 5.3.3** + Bootstrap Icons 1.11.3
- **jQuery** com validação unobtrusive
- **QuestPDF** e **DinkToPdf** — geração de PDFs
- **ClosedXML** — exportação para Excel
- **MailKit** — envio de e-mail

## Configuração Local

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/)

### Passos

1. Configure a string de ligação à base de dados via variável de ambiente:
   ```bash
   export ConnectionStrings__DefaultConnection="User Id=<user>;Password=<password>;Server=<host>;Port=5432;Database=sgeep"
   ```
   Em alternativa, crie o ficheiro `SGEEP.Web/appsettings.json` com base no exemplo `SGEEP.Web/appsettings.json.example`.

2. Aplique as migrações:
   ```bash
   dotnet ef database update --project SGEEP.Infrastructure --startup-project SGEEP.Web
   ```

3. Execute a aplicação:
   ```bash
   dotnet run --project SGEEP.Web
   ```

Na primeira execução, o sistema cria automaticamente os papéis e um utilizador administrador de base.

## Desenvolvimento

```bash
# Restaurar dependências e compilar
dotnet restore
dotnet build

# Executar testes
dotnet test

# Adicionar nova migração
dotnet ef migrations add <NomeDaMigracao> --project SGEEP.Infrastructure --startup-project SGEEP.Web
```

## Segurança

- Cabeçalhos de segurança HTTP configurados via middleware
- Redirecionamento HTTPS e HSTS em produção
- Rate limiting no login (10 pedidos/minuto)
- Bloqueio de conta após 5 tentativas falhadas (15 minutos)
- Middleware de alteração forçada de palavra-passe no primeiro login
- Tokens anti-CSRF em todos os formulários POST
