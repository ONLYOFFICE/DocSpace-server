---
paths:
  - "products/ASC.Files/Core/**/*.cs"
  - "products/ASC.Files/Worker/**/*.cs"
  - "common/services/ASC.ElasticSearch/**/*.cs"
  - "**/Search/**/*.cs"
  - "**/*Indexer*.cs"
---

# Full-text search: keep the index in step, never depend on it

Files, folders, metadata values and form data are indexed in OpenSearch (`ASC.ElasticSearch`:
`FactoryIndexer<T>`, `BaseIndexer<T>`; entities implement `ISearchItem`). The database is the truth;
the index only narrows the candidates. Any change to what is searchable, or to a write path of an
indexed entity, has an index side — and every read has to survive the index being absent.

## Reads: a SQL fallback is mandatory

- `TrySelectIdsAsync` / `TrySelectAsync` return `(false, [])` when the index is disabled,
  unreachable, missing or the query throws. Every caller handles `false` with an equivalent SQL path
  (`BuildSearch` / `LIKE`, the same filters) — never an empty result.
- **The selector's default limit is 1000** (`Selector._limit`), and the ids come back tenant-wide,
  before the folder/section filters are applied in SQL. Set `.Limit(0, BaseIndexer<T>.QueryLimit)`
  explicitly and treat `ids.Count >= QueryLimit` as an overflow that falls back to SQL, as
  `MetadataSearchQuery` does. Without that, matches past the cap disappear silently.
- Intersect the index ids with the SQL query; never return index documents as the answer.

## Writes: every change of an indexed value

- **Files and folders**: never call the indexer from a DAO or inside a transaction. After the commit,
  publish `FileIndexIntegrationEvent` / `FolderIndexIntegrationEvent`; `Files.Worker`
  (`IndexEventProcessingService`) applies them. A write that changes an indexed value needs its
  event: create, rename (`UpdateInfo`), move (`UpdateFolders` — for a folder, the ancestor chain of
  the **whole subtree** changes), restore, version replace, delete (all ids of the subtree).
- **Best effort**: an unreachable index must never fail or roll back the user's operation. Log
  through `[LoggerMessage]`; a bulk request answers 200 even when items are refused — read the item
  errors (`BaseIndexer.LogBulkErrors`). A lost event is picked up only by the periodic incremental
  `IndexAll` (by `ModifiedOn`), so the entity's `ModifiedOn` must move with the change.
- **New searchable field**: add it to the `ISearchItem` entity with its mapping attribute and to
  `GetSearchContentFields`. The mapping (`AutoMap`) is applied only when the index is created — an
  existing index needs `ReIndexAsync`, plan for it.

## Tenants and new indexers

- Documents carry `TenantId`; selects and `*ByQuery` calls add the tenant filter themselves, so index
  and query only inside a tenant set by `TenantManager` (background work sets it explicitly).
- A new indexer: `[Scope(typeof(IFactoryIndexer))]`, an incremental `IndexAllAsync` by `ModifiedOn`
  whose query joins tenants with `TenantStatus.Active`, a non-empty `SettingsTitle` (it is what the
  standalone search settings list), and deletion per tenant on portal removal.

## Tests

The index is asynchronous (event bus → `Files.Worker`), and the `integration-test` profile runs
OpenSearch without a data volume. Poll search results on a deadline (`tests.md`; at least 30 s in a
full parallel run), return the last observed state, and assert an absence only after a presence has
been seen. An empty **people** search is never index lag — that search is SQL only.
