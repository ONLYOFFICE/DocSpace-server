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

## Foreign keys to the tenant: Cascade, never Restrict

Removing a portal for good (`ITenantService.PermanentlyRemoveTenantAsync`, the last step of
`RemovePortalWorker`) deletes the `tenants_tenants` row and relies on the database to take
every row that belongs to the portal with it. One table whose FK to the tenant is
`ON DELETE RESTRICT` makes that delete fail as soon as the portal has a single row there. The
portal is then left half removed: its files are already wiped from storage, but the row is stuck in
`RemovePending`.

- A `HasOne(e => e.Tenant)...HasForeignKey(e => e.TenantId)` relationship is either left without
  `OnDelete` (a required FK defaults to Cascade) or set to `.OnDelete(DeleteBehavior.Cascade)`.
  **Never `DeleteBehavior.Restrict`** (or `NoAction`/`ClientSetNull`), in neither the MySQL nor the
  PostgreSQL section of the model. The two sections must agree.
- The same applies to an FK from a tenant-owned table to another tenant-owned parent (a config,
  a room, a user): the portal delete cascades through it, so a Restrict anywhere on the chain
  blocks it too.
- After generating, check the `MigrationContextModelSnapshot.cs` diff: a new
  `.OnDelete(DeleteBehavior.Restrict)` on a tenant relationship is a bug, not a detail.
