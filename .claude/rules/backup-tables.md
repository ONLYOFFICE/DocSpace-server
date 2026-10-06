---
paths:
  - "migrations/**/*.cs"
  - "common/Tools/ASC.Migrations.Core/**/*.cs"
  - "common/ASC.Data.Backup*/**/*.cs"
  - "common/Tests/ASC.Data.Backup.Core.Tests/**/*.cs"
  - "**/EF/**/*.cs"
---

# Backup and restore follow every schema change

A portal backup copies **only the tables some `*ModuleSpecifics` declares**
(`common/ASC.Data.Backup.Core/Tasks/Modules/`). A table nobody declares is silently missing from
the archive — the backup succeeds, the restored portal has lost that feature's data. So every
migration with `CreateTable`, `AddColumn`, `RenameTable`, `RenameColumn` or `DropTable` needs a
look at the backup declarations, in the same branch.

## A new table

1. **Declare it** in the module that owns the feature (`Tables` of `FilesModuleSpecifics`,
   `CoreModuleSpecifics`, `AiModuleSpecifics`, ...): `new TableInfo(name, tenantColumn, idColumn,
   IdType.X)` with the right `IdType` (`Autoincrement`, `Guid`, `GuidV7`, `Integer`; no id column for
   a composite key). Modules restore in the order of `ModuleProvider.AllModules`, and id mappings
   carry from one to the next — a parent table must be in the same module or an earlier one.
2. **User and date columns**: every column holding a user id goes into `UserIDColumns` (a row whose
   user cannot be mapped is dropped), every date column into `DateColumns`. Choose `InsertMethod`
   deliberately.
3. **References**: every column that holds another table's id needs a `RelationInfo` in
   `TableRelations` — with a collision resolver when the column is polymorphic (by `entry_type`),
   and `typeof(ParentModule)` when the parent lives in another module.
4. **No tenant column**: override `GetSelectCommandConditionText` with a join to a tenant-scoped
   parent (as `files_folder_tree` and `ai_chats_messages` do), otherwise backup throws
   `CantDetectTenant`.
5. **Ids inside strings or JSON, encrypted values**: they are not remapped by themselves — override
   `TryPrepareRow`/`TryPrepareValue` (and `PrepareData` for encrypted columns). If the ids cannot be
   remapped at all, do not back the table up; classify it instead (step 6).
6. **Not backed up on purpose**: add it to `BackupCoverageTests._intentionallySkipped` with the
   reason, or to `_knownGaps` with a ticket. A tenant table that is neither declared nor classified
   fails that test — never leave one unclassified.
7. **Register the entity in `MigrationContext`** (`common/Tools/ASC.Migrations.Core/EF/`), or the
   coverage test cannot see it.

## A changed table

- A new column on a declared table is copied automatically (`select t.*`, restore intersects with the
  target's columns) — but if it holds an id or a user id it still needs a relation or a
  `UserIDColumns` entry, and restore must cope with an older archive that lacks it.
- A rename or drop must update the declaration and both test registries; a stale entry fails
  `BackupCoverageTests` as well.

## Files in storage

Files are backed up only for storage modules in `PortalTaskBase.IsStorageModuleAllowed`. A feature
that stores files under a new module or domain adds it there; if the stored path embeds DB ids,
also add a case to `ModuleProvider.GetByStorageModule` and override `TryAdjustFilePath`.

## Verify

```bash
dotnet test common/Tests/ASC.Data.Backup.Core.Tests/ASC.Data.Backup.Core.Tests.csproj
```

`BackupCoverageTests` must pass. Per new table add a `FilesModuleSpecificsTests`-style case that takes
the table through `DeclaredTable()` and asserts that the tenant, id, user and relation columns are
remapped by `TryPrepareRow`. Model change to follow: `dd2a01e16b` (`files_order` — declaration, gap
list, test case in one commit). Its blind spot: tables without a `TenantId` property, and anything not
in `MigrationContext`, are not checked — review those by hand.
