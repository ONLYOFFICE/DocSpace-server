---
paths:
  - "products/ASC.Files/Core/Core/Dao/TeamlabDao/SecurityDao.cs"
  - "products/ASC.Files/Core/Core/Security/**"
  - "products/ASC.Files/Core/Utils/FileSharing.cs"
  - "products/ASC.People/Server/Api/**"
  - "common/ASC.Api.Core/Model/Employee*.cs"
  - "common/ASC.Api.Core/Webhook/Payloads/**"
  - "common/ASC.Core.Common/Users/UserExtensions.cs"
---

# Visibility filters: every source, every channel

A visibility filter is code that decides what a caller may learn: which accounts a list offers, whether an
account has access to an entry, which profile fields come back, what a search matches. Such a filter leaks
through the branch nobody looked at, not through the one under review. Bug 79163 took four review rounds:
each round closed one branch of `SecurityDao.FilterByKnownTo` or one field of its endpoint, and the next round
found the sibling - a Restrict record taken for membership in a third place, recipients of a file read without
checking that the caller may see them, a `shared` flag and an `includeShared` filter that told a reader what
the access list hid from it. Every one was in a table nobody had filled in.

Reviewers check the same points as matrix M8 of `review-branch` and cite them as `[visibility-filters.md §N]`.

## 1. State the rule before the code

Write down who may learn what, in one or two sentences, in the PR or the commit message: "a User or a Guest
learns about an account only when it shares a room, a relation or an access list it may see with it; it never
learns whom an entry is shared with unless it may see that entry's access list". The tables below are checked
against that sentence, not against the scenario of the bug report. A scenario covers one cell.

When `openspec/` exists and a requirement covers the endpoint, the sentence comes from it. A change of
visibility updates that requirement in the same change; the specs describe the server as it is, so a spec left
untouched describes the leak.

## 2. Table 1: every source of the result x every invariant

One row per place the result can come from: each `Union` branch, each list a controller passes in (an access
list, a set of known subjects), each fallback. One column per invariant that applies. The ones this codebase
already relies on:

| Invariant | Where it is decided |
|---|---|
| A Restrict record is no membership and no access - the caller's own, a member's of a room, a group's, one that takes a member of a group out of a room, one in an entry's access list. The `shared` flag is the exception: it marks any access record, a denying one included | `FileShare.Restrict` |
| The access list of an entry, and whether an account is on it, is told only to who may see that list: any reader of a room, or of an entry of a public room where the caller has determined the parent room type, otherwise who may manage the access | `FileSharing.CanSeeSharesAsync` |
| A group's manager row is no membership | `UserGroupRefType.Contains` |
| A room template is no room; a third-party room has no internal id to check; a room in the archive stays a room of its members | the `RoomTemplates` parent, `InternalEntryId` |
| A guest is offered only to whom it is related, or who sees it on the access list of the entry | `UserRelations`, the `shared` flag |
| A User or a Guest gets no contacts of another account | `UserManager.IsWithoutContactsAccessAsync` |

Every cell is `yes (path:line)`, `n/a (why)` or a finding.

## 3. Table 2: every output channel x caller x entry

The rows are what the response carries, each separately: the list, the total count, every per-row flag
(`shared`, an access level), every request filter that selects by hidden data (`includeShared`,
`excludeShared`, a type or inviter filter), what the search text is matched against, the profile fields. The
columns are the callers - Owner or DocSpace admin, Room admin, User, Guest, and for each of them the one who
manages the entry's access versus the one who may only read it - over the entry kinds: personal file, personal
folder, room, entry in a room, entry in a public room.

A cell says what that caller learns there and whether §1 allows it. An endpoint that needs only read access
has a reader column that must be filled in; that column is where 79163 leaked twice.

## 4. Tests and proof

- Each row of table 2 that the change touches has a test with the weakest caller who could see too much: a
  reader, not the owner; a User, not an admin. Assert the absence after a presence seen in the same test, so
  the test cannot pass by an empty setup.
- Prove each test red with `.claude/scripts/prove-red.sh` (`tests.md`).
- A cell left without a test goes into the PR as "not verified", never silently.

## 5. A finding against a filter

- **Fixing**: the class is the table, not the cited cell. Fill both tables again for the whole method and
  endpoint before handing the fix back (`fixing-findings.md` §1).
- **Rejecting**: a rejection needs the same evidence as a fix - the cell for every caller and entry kind the
  finding names, read in the code. A rejection argued for one entry kind ("the owner already sees it") is not
  one when the finding names another (any member of a room sees a room's access list).
