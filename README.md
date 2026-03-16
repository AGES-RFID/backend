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
- Docker (ou PostgreSQL local para desenvolvimento)


Clone o repositório e navegue até o diretório backend:
   ```bash
   cd backend
   ```

Copie a configuração do ambiente para `src`:
   ```bash
   cp .env.example src/.env
   ```
  
Execute o banco de dados PostgreSQL usando Docker:
   ```bash
   docker compose up # dica: use `-d` para rodar em segundo plano
   ```

Inicie o servidor de desenvolvimento:
   ```bash
   dotnet run --project src
   ```

A API estará disponível em `http://localhost:5000` com documentação Swagger em `http://localhost:5000/swagger`.
