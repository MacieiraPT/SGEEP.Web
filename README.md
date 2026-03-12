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

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/)

## Configuração

1. Clone o repositório:
   ```bash
   git clone <url-do-repositório>
   cd SGEEP.Web
   ```

2. Configure a string de ligação à base de dados via variável de ambiente:
   ```bash
   export ConnectionStrings__DefaultConnection="User Id=<user>;Password=<password>;Server=<host>;Port=5432;Database=sgeep"
   ```
   Ou use o ficheiro `SGEEP.Web/appsettings.json` (não rastreado pelo git) com base no exemplo disponível em `SGEEP.Web/appsettings.json.example`.

3. Aplique as migrações:
   ```bash
   dotnet ef database update --project SGEEP.Infrastructure --startup-project SGEEP.Web
   ```

4. Execute a aplicação:
   ```bash
   dotnet run --project SGEEP.Web
   ```

A aplicação estará disponível em:
- HTTP: `http://localhost:5267`
- HTTPS: `https://localhost:7110`

Na primeira execução, o sistema cria automaticamente os papéis e um utilizador administrador de base (ver `SGEEP.Web/Data/SeedData.cs`).

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

## Testes

Os testes unitários cobrem validação de entidades, lógica de negócio, paginação e serviços CRUD. Utilizam uma base de dados InMemory para isolamento.

```bash
dotnet test
```

## Segurança

- Cabeçalhos de segurança HTTP configurados via middleware
- Redirecionamento HTTPS e HSTS em produção
- Rate limiting no login (10 pedidos/minuto)
- Bloqueio de conta após 5 tentativas falhadas (15 minutos)
- Middleware de alteração forçada de palavra-passe no primeiro login
- Tokens anti-CSRF em todos os formulários POST

## Notas para Contribuição

- Todo o código, comentários e mensagens de interface devem ser escritos em **português**
- Siga as convenções de nomenclatura existentes (PascalCase para classes/métodos, camelCase para variáveis)
- O ficheiro `appsettings.json` está no `.gitignore` — nunca inclua credenciais no repositório
- Corra os testes localmente antes de fazer push (`dotnet test`)
