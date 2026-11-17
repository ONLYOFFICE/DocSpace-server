# Design

## Context

See proposal.md - Why. The design relies on these existing requirements:

- `billing` "Single tariff of a self-hosted installation": an Enterprise installation without an activated licence
  gets its own 30-day trial tariff on the first read of the tariff. That trial is the vehicle: the change never
  invents another tariff, it only ends this one earlier.
- `branding-and-licensing` "Activating a license" and "Refreshing the license": a license file, once accepted,
  writes the installation's quota and tariff, and the daily background re-read keeps it current. Both stay the only
  way a license from a file comes into force.
- The editing service's `license` command, already used to confirm a license file: while the editing service has no
  license file, it answers with its own trial (`trial` set, an end date, and an active or expired result).

## Goals / Non-Goals

**Goals:**

- Decide "is the editing service on its trial" from the `license` command only, with an unknown answer never read
  as "no trial" or "trial expired".
- Keep one writer of the license quota and tariff at a time, so a trial write never overwrites a license just
  accepted.
- Answer the anonymous `getIsLicenseRequired` without a request to the editing service per call.

**Non-Goals:**

- Carrying a paid license of the editing service over without a file: only its trial is followed.
- A new audit event for a wizard completed on the trial.
- Changing the client: it already hides the upload step when `getIsLicenseRequired` is false and shows the trial
  banner from the tariff.

## Decisions

- **The trial is the installation's own trial quota, not a new one.** The quota the installation already creates on
  the first tariff read stays in force; only the tariff end comes from the editing service. Alternative: a separate
  "editing service trial" quota. Rejected: it adds a state the client and the billing requirement do not know, and
  a clean installation already has the existing one before the wizard runs.
- **The end only moves closer, compared by day.** The editing service moves its trial end by seconds on restart and
  starts a fresh trial when it is reinstalled; comparing by day and never moving the end later keeps repeated
  re-reads from rewriting the tariff and keeps a reinstall from extending the trial. An installation trial ending
  later on the same day is kept, so it is not cut to midnight.
- **Only an answer that says `trial` is a trial.** The editing service answers "expired trial" also for a license
  file it refuses, without the `trial` flag; such an answer, an error, an unreadable body or a missing date are
  "not a trial" or "unknown", never "expired".
- **Unknown changes nothing.** The background re-read leaves the tariff as it is. The wizard keeps the
  installation's own trial with its current end and records the acceptance, but still requires a file when that
  trial is not in force or has ended, or the last answer known said the editing service runs no trial or its trial
  ended. The anonymous question uses the last answer of the past 2 days, or answers true with none.
- **A trial that would already be over is no trial.** The wizard and the anonymous question take the editing
  service's trial only when the end it leads to (the earlier of the installation's end and the editing service's)
  is today or later; otherwise a file is required, as before the change. The re-read still writes that end.
- **The answer lives in the distributed cache with fail-safe.** It is one value per installation: the background
  re-read in the worker refreshes it, the API nodes read it. The default `IFusionCache` (memory, Redis and the
  backplane) shares it with every node and keeps it over a restart; the `"memory"` cache would keep it in one
  process. An answer is reused for 15 minutes, the last one kept for 2 days (longer than the daily re-read) while
  the editing service fails, and asked again once a minute; the license command keeps its own 5-second limit. A
  fresh question with a known answer replaces the cached value for every node; an unknown one leaves it alone.
  Alternative: no cache. Rejected: an anonymous endpoint would send one request to the editing service per call.
  Alternative: a setting in the database. Rejected for now: the cache with a 2-day fail-safe covers the restart,
  and a lost answer only makes the wizard fall back to the installation's own trial.
- **A tariff write tells the open clients.** A write that changed the tariff sends `s:change-tariff` to the quota
  room of the portal, or of every active portal for the shared tariff of tenant -1, so the trial banner and the
  payment page read the new end. A failed send is logged and does not fail the write. A tariff that becomes not
  paid only because its day passed is not a write and sends nothing.
- **One lock for the license writers.** The trial write and the license-file write take the same distributed lock,
  and the trial checks the license file and the license quota under it.
- **Completing the wizard counts as acceptance.** The user agrees to the license terms in the wizard, so a license
  file that later appears on the license path is applied by the background re-read without a separate accept.

## Risks / Trade-offs

- [Existing installations on their 30-day trial without a license file get an earlier end, or none left, on the
  next re-read] → Intended: the portal stops being usable when the editors do. The proposal names it under
  "Breaks current behaviour"; the trial banner shows the new end before it is reached.
- [An installation restored from a backup loses its license file and, next to an editing service whose trial has
  expired, becomes not paid at once instead of after 30 days] → Accepted by decision; the license page lets the
  admin upload the file immediately.
- [The editing service's answer can be up to 15 minutes old after a change there] → A fresh question by the wizard
  or the re-read replaces it for every node; while the editing service fails, its last answer of the past 2 days is
  used.
- [Until the wizard is completed while the editing service answers, or the first re-read runs, the installation's
  trial keeps its 30-day end, later than the editing service's] → Accepted: the next daily re-read brings it closer.
- [Without Redis there is no shared cache] → Each process then keeps its own copy, as the rest of the server's
  caches do; the re-read and the wizard still ask the editing service themselves.
- [A trial write that started before an accept finishes after it] → The shared lock orders them; the accept always
  ends with the license in force.

## Migration Plan

No data migration. Existing data stays consistent with the modified requirements:

- An installation with a license from a file keeps it: the trial never replaces the license quota.
- An installation on its own trial keeps that quota; only its tariff end can move closer on the next re-read.
- Rollback: the previous server version reads the same quota and tariff rows; a trial end moved closer stays as it
  is until a license is accepted.
