# 💈 Barbearia — Gerenciador de Agendamentos

Sistema completo de agendamentos para barbearias, construído do zero como projeto de aprendizado full-stack em **C# / .NET**, cobrindo modelagem de dados, regras de negócio, testes automatizados, observabilidade e deploy em produção.

## 🔗 Aplicação em produção

- **Front-end:** _(preencher após o deploy)_
- **API:** _(preencher após o deploy)_

## 🏗️ Arquitetura

O projeto é dividido em duas aplicações independentes que se comunicam via HTTP/JSON:

```
Barbearia.Web (Blazor WebAssembly)  --->  Barbearia.Api (ASP.NET Core Minimal API)  --->  PostgreSQL
```

- **`Barbearia.Api`** — back-end responsável pelas regras de negócio (criar, listar horários disponíveis e cancelar agendamentos) e pela persistência dos dados via Entity Framework Core.
- **`Barbearia.Web`** — front-end em Blazor WebAssembly, hospedado como site estático, que consome a API.
- **`Barbearia.Api.Tests`** — testes unitários (xUnit) cobrindo as regras de negócio do `AgendamentoService`.

## 🧰 Stack

| Camada | Tecnologia |
|---|---|
| Back-end | C# / .NET 10, ASP.NET Core Minimal APIs |
| Front-end | Blazor WebAssembly |
| Banco de dados | PostgreSQL (via Npgsql + Entity Framework Core) |
| Testes | xUnit + EF Core InMemory |
| Logging | `ILogger` estruturado |

## 📁 Estrutura de pastas

```
├── src/
│   ├── Barbearia.Api/       # Back-end (regras de negócio, API, acesso a dados)
│   │   ├── Models/          # Entidades: Cliente, Barbeiro, Agendamento
│   │   ├── Data/            # DbContext
│   │   ├── Services/        # Regras de negócio (AgendamentoService)
│   │   └── Migrations/      # Histórico de mudanças do banco (EF Core)
│   └── Barbearia.Web/       # Front-end (Blazor WebAssembly)
└── tests/
    └── Barbearia.Api.Tests/ # Testes unitários
```

## ✨ Funcionalidades

- Listar horários disponíveis de um barbeiro em uma data (considerando horário de funcionamento e agendamentos já existentes)
- Criar um agendamento (com validação de conflito de horário)
- Cancelar um agendamento

## 🚀 Rodando o projeto localmente

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) rodando localmente

### 1. Configurar o banco de dados

Crie um banco e um usuário dedicado:

```sql
CREATE DATABASE barbearia_db;
CREATE USER barbearia_app WITH PASSWORD 'sua-senha-aqui';
GRANT ALL PRIVILEGES ON DATABASE barbearia_db TO barbearia_app;
```

No PostgreSQL 15+, também é necessário liberar o schema:

```sql
GRANT ALL ON SCHEMA public TO barbearia_app;
```

### 2. Configurar a string de conexão

Dentro de `src/Barbearia.Api`, configure a conexão via User Secrets (nunca commitada no repositório):

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:BarbeariaDb" "Host=localhost;Database=barbearia_db;Username=barbearia_app;Password=sua-senha-aqui"
```

### 3. Aplicar as migrations

```bash
cd src/Barbearia.Api
dotnet ef database update
```

### 4. Rodar a API

```bash
dotnet run --project src/Barbearia.Api
```

### 5. Rodar o front-end (em outro terminal)

```bash
dotnet run --project src/Barbearia.Web
```

## ✅ Rodando os testes

```bash
dotnet test tests/Barbearia.Api.Tests
```

Para ver o relatório de cobertura de código:

```bash
dotnet test tests/Barbearia.Api.Tests --collect:"XPlat Code Coverage"
```

## 📚 Sobre o projeto

Este projeto foi construído de forma incremental, dia a dia, com foco em boas práticas de engenharia de software: separação de responsabilidades (regras de negócio isoladas em uma camada de serviço, independente da API), testes unitários com 100% de cobertura nas regras de negócio, logging estruturado para observabilidade e um pipeline de deploy real em produção.
