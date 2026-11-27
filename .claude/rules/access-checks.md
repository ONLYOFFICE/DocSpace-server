---
paths:
  - "**/*Controller.cs"
  - "products/*/Core/**/*.cs"
  - "products/*/Server/**/*.cs"
  - "products/*/Worker/**/*.cs"
  - "web/ASC.Web.Api/**/*.cs"
  - "web/ASC.Web.Core/**/*.cs"
  - "web/ASC.Web.Studio/**/*.cs"
  - "common/services/**/*.cs"
  - "common/ASC.Core.Common/Security/**/*.cs"
  - "common/ASC.Data.Reassigns/**/*.cs"
---

# Access checks: the stored entry, every case, every holder

`visibility-filters.md` decides what a caller may learn; this rule decides what a caller may do. An
access check is written for the case in front of the author and passes the case next to it: an
existing record changed through a shortcut made for creating one, the entry kind the check was not
written for, ids handed to a worker that acts for someone else, a release that takes out other
users' entries. The order of check and write is `failure-paths.md` §1. Reviewers check the same
points as lens E of `review-branch` and cite them as `[access-checks.md §N]`.

## Checklist

- The check decides on the stored record the action changes, not on what the request says about it (§1)
- A shortcut past the general check covers only its own case; one made for create checks nothing exists (§1)
- Every case of a switch over entry kind, room type or action that writes has a check; the rest refuses (§2)
- Request ids are checked for the caller in one batch, under a named limit, before use or queueing (§3)
- Queued work acts as the caller; a wider account acts only on what the caller's check passed (§3)
- A removal from state several holders share removes only the caller's own entries (§4)
- A token from a link authorizes an action only when it is keyed, bound to its purpose and expiring (§5)

## 1. Decide on what is stored

- When the request names an existing record (an id), read it first and decide on its stored kind,
  owner and access. A request field that describes the record (its type, its access level, a
  "primary" flag) is the caller's claim, not a fact.
- A shortcut past the general check (a reader may create the first link, a member may change its own
  state) names the one case it is for and checks that case: for a create, that nothing with that id
  or role exists yet. An update of an existing record goes through the general check.

## 2. Every case has its check

- A check inside one case of a `switch` or an `if` chain on the room type, the entry kind or the
  requested action is the check of that case only. Put it before the branch, or give every case one;
  the fall-through case refuses.
- For a method that writes, list the cases in the commit or the PR, with the right each one needs and
  the line that checks it. A case without a line is a finding.

## 3. Ids from the request, work handed on

- Every id of a request list is checked for the caller, for the right the action needs, before it is
  used, counted or queued, and the list has a named upper limit. A read filter lets through ids the
  caller may read but not change. `FileSecurity` has batch calls for reading (`FilterReadAsync`,
  `CanReadAsync` over the entries), downloading (`FilterDownloadAsync`) and deleting (`CanDeleteAsync`
  over the entries); a move or a share is checked entry by entry (`CanMoveAsync`, `CanShareAsync`),
  with the entries loaded in one batch.
- A background task acts under the account that asked for it. A wider account (the portal owner, a
  system account standing in for an anonymous caller) acts only on ids that passed the caller's check.

## 4. State with several holders

- Editing sessions, tracker entries, locks and marks can belong to several users, tabs or sessions at
  once. A removal takes out the caller's own entries, by user and session or tab, never every entry
  of the object, and it returns what it removed when a later step decides on that.
- "The caller is one of the holders" is not "the caller is the only holder": decide on the others
  before an overwrite.
- All anonymous callers share one account, `Constants.Guest` (`common/ASC.Core.Common/Configuration`);
  "own" for them is the session or the tab, never the account id.

## 5. Tokens in links

- An id or a key that arrives in a link and authorizes an action (confirm, unsubscribe, deactivate)
  is checked before the action: a keyed hash or a signature over the whole value, with an expiry, as
  `EmailValidationKeyProvider` builds and validates keys, over a value that names the address and
  the action. An encrypted value is not a checked one: a changed ciphertext can still decrypt to a
  value that parses.
- The more the action reaches (every account of an address, every region), the more the token binds.
  A risk kept on purpose goes into the PR's Known limitations with the reason.
