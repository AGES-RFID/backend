# RFID Backend

## Stack

- **Runtime**: .NET 10.0
- **Framework**: ASP.NET Core
- **ORM**: Entity Framework Core
- **Bancos de Dados**: PostgreSQL
- **Documentação da API**: Swagger/OpenAPI

## Quickstart

### Pré-requisitos

- .NET 10.0 SDK
- Docker (para subir o PostgreSQL de desenvolvimento)

Clone o repositório e navegue até o diretório backend:

```bash
cd backend
```

### Fluxo padrão (recomendado): .NET local + PostgreSQL no Docker

1. Suba apenas o banco de dados:

```bash
docker compose up -d
```

2. Rode a API localmente:

```bash
dotnet run --project Backend.API
```

A API estará disponível em `http://localhost:5000` com documentação Swagger em `http://localhost:5000/swagger`.

### Rodar tudo no Docker (opcional)

Suba API + PostgreSQL usando o profile `full`:

```bash
docker compose --profile full up -d --build
```

## Testes

Os testes do backend são organizados em dois projetos: `Backend.Tests.Unit` e `Backend.Tests.Integration`. Ambos os projetos utilizam `xUnit` como framework de teste.

### Testes unitários

Para rodar os testes unitários:

```bash
dotnet test Backend.Tests.Unit
```

### Testes de integração

> Garanta que o docker está rodando, pois os testes de integração executam um testcontainer do PostgreSQL para validar as operações de banco de dados.

Para rodar os testes de integração:

```bash
dotnet test Backend.Tests.Integration
```
