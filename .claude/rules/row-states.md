---
paths:
  - "**/EF/**/*.cs"
  - "**/Dao/**/*.cs"
  - "**/Database/**/*.cs"
  - "common/ASC.Core.Common/Data/**/*.cs"
  - "**/Data/**/*.cs"
  - "**/Repositories/**/*.cs"
  - "**/*Repository.cs"
  - "**/*Dao.cs"
  - "**/*DbContext.cs"
  - "**/DbWorker.cs"
  - "migrations/**/*.cs"
---

# A new state of stored rows

A feature adds a state to rows that already have readers: a draft becomes deleted, a message sent,
an item claimed. The new code checks the state; the queries written before it do not know it is
there, and neither do the DTO, the export and the cleanup that read the same columns. Reviewers cite
this rule as `[row-states.md §N]`.

## Checklist

- A new state has a column of its own (nullable, a flag, an enum), never a magic value in another column (§1)
- Every query on the table — reads, `ExecuteUpdate`, `ExecuteDelete`, counts — is listed and decides on the state (§2)
- A write that moves a row between states has the expected state in its `Where` and reads the row count (§2)
- Every output of the rows (DTO, webhook, export, index) shows the state or leaves the row out (§3)
- Each old query that must not see the state has a test with a row in that state, proven red (§4)

## 1. A column of its own

- A "deleted" or "sent" state written as a sentinel into a column with a meaning of its own (a
  creation date of 1970, an id of -1, an empty string) is read as that meaning by every reader that
  does not know the sentinel: the DTO publishes it, a sort puts it first, a retention window counts
  from it.
- Add a nullable column (`DeletedOn`), a flag or an enum, with its migration and its backup
  declaration (`backup-tables.md`).
- Seen in review: a deletion mark written into a creation date reached the API as a date of 1970.

## 2. Every query decides

- Before writing the first query that checks the new state, list every query on the table:
  `findReferences` on the context's `DbSet` and on the entity's query methods. Each one filters by
  the state, is meant to see every state (a cleanup), or is a finding.
- A write that moves a row between states (bind, claim, mark sent) carries the state it expects in
  its `Where` (`DeletedOn == null`, the target still empty) and checks the number of rows it changed.
  Zero means the row was not in that state: the caller refuses before any later step (a file move).
- Seen in review: the save path refused a deleted draft, while an older binding query accepted it and
  made it sent for good.

## 3. Every output

- A row in the new state either carries it outside (a field the DTO documents) or is left out of
  lists, counts, webhooks, exports and the search index.

## 4. Guard

- For each query in the §2 list that must not see the state, a test puts a row into the state and
  calls the path: refused or not listed. Prove it red (`tests.md`).
