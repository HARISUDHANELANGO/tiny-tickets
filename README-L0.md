# Level 0 – Foundation Setup Log

| Step | What I did                     | What I observed                    | What I learned                |
| ---- | ------------------------------ | ---------------------------------- | ----------------------------- |
| 1    | Created folder & git init      | `.git` appeared                    | Git starts local history      |
| 2    | Installed Node & Angular CLI   | `ng version` shows v20             | Angular CLI scaffolds apps    |
| 3    | Installed .NET 8 SDK           | `dotnet new` lists templates       | SDK compiles C# apps          |
| 4    | Installed Docker               | `docker info` shows engine running | Containers run isolated apps  |
| 5    | Configured Git                 | Name/email set globally            | Enables commits & pushes      |
| 6    | Installed VS Code + extensions | Syntax highlighting works          | Editor ready for Angular + C# |

# TinyTickets — Full-Stack Learning Journey

A practical micro-project built to master Azure, Docker, Kubernetes, and DevOps concepts step-by-step.

---

## Overview

TinyTickets is a lightweight full-stack application designed to learn the complete flow of:

- Angular 20 (Frontend)
- .NET 8 Web API (Backend)
- SQL Server with EF Core
- Docker → Kubernetes → Azure (upcoming)

The app allows creating and listing simple “tickets,” evolving from an in-memory demo to a persistent SQL-backed solution.

---

# Progress Levels

---

# LEVEL 0 – Environment Setup

| Tool               | Purpose                | Status       |
| ------------------ | ---------------------- | ------------ |
| Node + npm         | Angular CLI & builds   | ✓ Installed  |
| Angular CLI (v20)  | Scaffolds SPA          | ✓ Installed  |
| .NET 8 SDK         | Builds backend         | ✓ Installed  |
| Docker Desktop     | Containers (Level 3 +) | ✓ Installed  |
| Git + VS Code      | Versioning & editing   | ✓ Configured |
| Visual Studio 2022 | Runs API (Kestrel)     | ✓ Ready      |

Copy & Learn Principle:  
Each command is executed, observed, and documented to understand “why,” not just “how.”

---

# LEVEL 1 – Hello World Full-Stack (Angular ↔ .NET API)

## Frontend (Web)

- Angular 20 standalone app (`web/`)
- Used `bootstrapApplication()` and `provideHttpClient()`
- Simple UI: add a ticket + list tickets

## Backend (API)

- .NET 8 Minimal API (`TinyTickets.Api/`)
- Endpoints:
  - `GET /tickets`
  - `POST /tickets`
- Enabled CORS
- Ran via Kestrel

**Result:**  
Angular (4200) ↔ API (5080) communication working end-to-end.

---

# LEVEL 2 – Database Persistence with SQL + EF Core

Goal: Store tickets in SQL Server

## Key Changes

1. Added EF Core packages

   - `Microsoft.EntityFrameworkCore`
   - `Microsoft.EntityFrameworkCore.SqlServer`
   - `Microsoft.EntityFrameworkCore.Tools`

2. Added Ticket entity:

```csharp
public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

3. Created DbContext
4. Ran migrations locally
5. Updated API to use SQL-backed persistence

---

# LEVEL 3 – Docker Containerization (Web + API + SQL Server)

## 3.1 Folder Structure

```
tiny-tickets/
│
├─ TinyTickets.Api/
│   ├─ Program.cs
│   ├─ AppDbContext.cs
│   ├─ Migrations/
│   ├─ TinyTickets.Api.csproj
│   └─ Dockerfile
│
├─ web/
│   ├─ src/
│   ├─ dist/web/browser/
│   └─ Dockerfile
│
└─ docker-compose.yml
```

This structure ensures Docker COPY commands work correctly.

---

## 3.2 API Dockerfile (Multi-Stage Build)

```
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TinyTickets.Api.dll"]
```

---

## 3.3 Angular Dockerfile (Nginx Hosting)

```
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Angular 20 outputs to `dist/web/browser`.

---

## 3.4 SQL Server Container

```
db:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    - ACCEPT_EULA=Y
    - SA_PASSWORD=YourStrong@Passw0rd
  ports:
    - "1433:1433"
  volumes:
    - sqldata:/var/opt/mssql
```

---

## 3.5 docker-compose.yml (All Three Services)

```
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: tinytickets_db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  api:
    build:
      context: ./TinyTickets.Api
      dockerfile: Dockerfile
    container_name: tinytickets_api
    depends_on:
      - db
    environment:
      - ConnectionStrings__DefaultConnection=Server=db,1433;Database=TinyTicketsDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
      - ASPNETCORE_URLS=http://+:8080
    ports:
      - "5080:8080"

  web:
    build:
      context: ./web
      dockerfile: Dockerfile
    container_name: tinytickets_web
    depends_on:
      - api
    ports:
      - "4200:80"

volumes:
  sqldata:
```

---

## 3.6 Auto-Applying EF Core Migrations

Add this right after `var app = builder.Build();`:

```
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

This ensures DB + tables are created automatically inside Docker.

---

## 3.7 CORS Final Fix

Remove:

```
app.UseCors();
```

Keep:

```
app.UseCors("AllowClient");
```

---

## 3.8 Final Outcome

- Angular served via Nginx container
- .NET API running in ASP.NET runtime container
- SQL Server running in its own container
- API connects to SQL using `Server=db,1433`
- EF migrations run automatically on startup
- Angular communicates with API successfully
- No CORS issues

- Full stack runs with:

```
docker-compose up --build
```

---

# End of Levels 0–3

LEVEL 4 – Azure Deployment (UI + API + Database)

Goal: Move TinyTickets from local Docker into Azure (Frontend, Backend, Database working end-to-end)

4.1 Deploy API to Azure App Service

| Step | What I did                                        | What I observed              | What I learned                      |
| ---- | ------------------------------------------------- | ---------------------------- | ----------------------------------- |
| 1    | Created Azure App Service (.NET 8)                | API deployed successfully    | App Service hosts backend easily    |
| 2    | Added Azure SQL connection string in App Settings | API connected to cloud DB    | App Settings override app config    |
| 3    | Enabled “Allow Azure Services” + firewall IP      | API could access SQL         | Azure SQL connectivity rules matter |
| 4    | Tested `/tickets`                                 | Received correct JSON output | API works publicly                  |

4.2 Deploy Angular App to Azure Static Web Apps

| Step | What I did                                      | What I observed                 | What I learned                     |
| ---- | ----------------------------------------------- | ------------------------------- | ---------------------------------- |
| 1    | Created a Static Web App from portal            | GitHub workflow auto-created    | Azure SWA auto-generates pipelines |
| 2    | Build failed (Node version mismatch)            | Oryx forced Node 18             | Angular 20 requires Node 20        |
| 3    | Switched to manual GitHub build                 | Dist folder generated correctly | SWA can skip its own build         |
| 4    | Updated workflow to deploy only built artifacts | Deployment succeeded            | Understanding dist path is crucial |
| 5    | Opened SWA URL                                  | App loaded successfully         | UI hosted globally                 |

4.3 End-to-End Integration (UI → API → SQL)

| Step | What I did                                              | What I observed              | What I learned             |
| ---- | ------------------------------------------------------- | ---------------------------- | -------------------------- |
| 1    | Updated Angular environment API URL → Azure App Service | UI could call API            | Environment config is key  |
| 2    | Allowed SWA domain in CORS (App Service)                | 200 OK responses             | CORS is domain-specific    |
| 3    | Tested creating + listing tickets                       | Data inserted into Azure SQL | Entire cloud chain working |

4.4 Result

Angular UI hosted on Azure Static Web Apps

API hosted on Azure App Service

Database hosted on Azure SQL

All three connected and functioning

TinyTickets is now fully cloud-hosted
