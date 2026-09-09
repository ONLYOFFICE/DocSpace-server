---
name: db-migrations
description: "Creating and applying DocSpace DB migrations (EF Core, MySQL/PostgreSQL, SaaS/Standalone). USE FOR: create a migration, add migration, change database schema, apply migrations, generate EF migration. DO NOT USE FOR: data migration between instances (ASC.MigrationPersonalToDocspace), code migrations."
---

# DB Migrations

## Layout

- Migrations exist in **4 parallel variants**: `migrations/{mysql,postgre}/{SaaS,Standalone}`.
  A schema change must land in all variants — touching only one of them is almost always a mistake.
- Migration projects are collected in `ASC.Migrations.sln`.
- EF entity models live in the main projects (`ASC.Core.Common`, etc.) — **never create duplicate
  entity classes** for tables that already have models.

## Creating a migration

Do not write migrations by hand — generate them with `common/Tools/ASC.Migration.Creator`:

1. Change the EF model in the main project.
2. Check `common/Tools/ASC.Migration.Creator/appsettings.creator.json`: by default `Providers`
   contains only `MySql` — to generate the postgre variant, add a `PostgreSql` provider entry
   with a working connection string.
3. `cd common/Tools/ASC.Migration.Creator && dotnet run` — generates migrations for every
   configured provider and places them into the `ASC.Migrations.sln` projects.

## Applying

```bash
cd common/Tools/ASC.Migration.Runner && dotnet run
```

## Rules

- Never edit an already-applied migration — always add a new migration on top.
- After generation, verify the changes landed in both the mysql and postgre variants.
