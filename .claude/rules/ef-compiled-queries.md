---
paths:
  - "**/EF/**/*.cs"
  - "**/Dao/**/*.cs"
  - "common/ASC.Core.Common/Data/**/*.cs"
  - "common/ASC.AI.Integration/Database/**/*.cs"
---

# Database queries are precompiled

A DAO (or a `Db*Service`) never writes LINQ against a `DbSet` inline. Every read, `ExecuteDelete`
and count goes through a **compiled query** that the context exposes as a method, the way the Files
DAOs, `DbTenantService` and the AI integration do. A query written inline is translated from scratch
on every call and fails to translate only when a request finally reaches it; a compiled one is
translated once and is exercised at service start.

## The shape

One file per area, next to the others (`products/ASC.Files/Core/Core/EF/Queries/`,
`common/ASC.Core.Common/EF/Queries/`, `common/ASC.AI.Integration/Database/`), holding two parts:

```csharp
public partial class FilesDbContext
{
    [PreCompileQuery]
    public Task<DbFilesProject> ProjectAsync(int tenantId, int projectId)
    {
        return ProjectQueries.ProjectAsync(this, tenantId, projectId);
    }
}

static file class ProjectQueries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFilesProject>> ProjectAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int projectId) =>
                ctx.Projects.FirstOrDefault(r => r.TenantId == tenantId && r.Id == projectId));
}
```

- The context method is **public, an instance method and marked `[PreCompileQuery]`** — that
  attribute is what `WarmupBaseDbContextStartupTask` (`common/ASC.Core.Common/EF/Context/BaseDbContext.cs`)
  looks for. The compiled delegate lives in a `static file class`, so nothing outside the file calls it
  around the context method.
- The DAO calls the context method only: `await filesDbContext.ProjectAsync(tenantId, id)`.
- The **tenant id is a parameter**, passed explicitly — a compiled query captures no ambient state.
- Return `Task<T>` for a single row, an `Any` or a `Count`, `IAsyncEnumerable<T>` for a list, and
  `Task<int>` for an `ExecuteDelete()` (inside the compiled lambda it is the synchronous
  `ExecuteDelete`, not `ExecuteDeleteAsync`).

## A compiled query has one fixed shape

It cannot be composed per call, so the things a hand-built query does with `if` are expressed as
parameters:

- **Optional filter** — a nullable parameter checked inside the predicate:
  `(createBy == null || r.CreateBy == createBy) && (lowerText == null || r.Title.ToLower().Contains(lowerText))`.
  Normalize the input in the DAO (blank → null, lowered text) before the call.
- **A set of ids** — an `IEnumerable<T>` parameter with `ids.Contains(r.Id)`.
- **Paging** — `int` parameters into `Skip(offset).Take(count)`; always with a deterministic
  `OrderBy…ThenBy` on a unique column, or pages overlap.
- When the variants differ in more than a predicate (a different join, a different projection),
  write one compiled query per variant rather than one query full of switches.

## Exceptions: not precompiled, and saying why

Some operations cannot be compiled. They still go into the same `Queries` file — as a **plain
public context method without `[PreCompileQuery]`** (`ConvertMetadataCascadeLinksToDirectAsync`), or
as an uncompiled delegate in the static class (`UpdateVectorizationsDeletedOnAsync`) — with a comment
that starts "Not precompiled on purpose:" and gives the reason, so the next reader does not "fix" it
into a broken compiled query:

- **`ExecuteUpdate`** fails to translate inside `EF.CompileAsyncQuery`; it surfaced once as a 403 on
  every un-cascade (`ConvertMetadataCascadeLinksToDirectAsync`, `UpdateVectorizationsDeletedOnAsync`).
- **Genuinely dynamic predicates** — an `Or` chain built from a runtime list
  (`AbstractDao.BuildSearch` over several words), a predicate builder.
- **Multi-step work** on the caller's context and transaction that mixes reads with tracked changes.

Inserts through `AddAsync` + `SaveChangesAsync` are not queries and stay in the DAO.

## The warmup runs every `[PreCompileQuery]` method

At start-up the warmup invokes each one with default arguments — `0`, `null` strings, `Guid.Empty`,
the **underlying** default of a nullable (so a `Guid?` arrives as `Guid.Empty`, not null), empty
collections — inside a transaction it rolls back. So:

- a compiled `ExecuteDelete` is safe there, the rollback undoes it; never mark anything with side
  effects outside the database (a cache, an event) as `[PreCompileQuery]`;
- a query that fails to translate does **not** crash the service: the warmup logs a warning named
  after the method and moves on, and the request that needs the query fails later.

## Verify

After adding or changing compiled queries, build, start the service (a test run is enough) and read
its main log — `../Logs/test/<service>.log` after a test run, `../Logs/<service>.log` locally. A query
that fails to translate is written there as a `WARN` line naming the method, followed by the exception:

```
...|WARN|[22]|ASC.Core.Common.EF.WarmupBaseDbContextStartupTask - WebhooksLogAsync|MySqlConnector.MySqlException ...
```

No such line for your methods is what you want:

```bash
grep "WarmupBaseDbContextStartupTask" ../Logs/test/files.log
```

The task runs in the background (`IStartupTaskNotAwaitable`) and prints no "completed" line, so an
empty result means "nothing failed", not "it did not run".

Then exercise every new method through a test: the warmup only proves that the query translates,
not that it returns the right rows.
