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
- Changing the client for the trial itself: it already hides the upload step when `getIsLicenseRequired` is false
  and shows the trial banner from the tariff when a page loads. A page that is already open reads a changed tariff
  only after the frontend subscribes to `s:change-tariff`, which is left to the frontend.

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
  trial is not in force or has ended, or the last answer known said the editing service runs no trial, that its
  trial ended, or that it runs a trial that would leave no trial in force today once applied: the same answers make
  the anonymous question ask for a file. The anonymous question uses the last answer of the past 3 days, or answers
  true with none; with none known the wizard still completes on the own trial, by decision, since an editing
  service that never answered is not read as one without a trial.
- **The decisions are functions of what was read.** Whether the wizard page asks for a file, whether the wizard
  completes on its own trial while the editing service is silent, how an answer or its absence is read, and how a
  tariff write fans out to its portals are functions without I/O (`DocsTrialDecision`,
  `DocumentServiceLicense.ClassifyAnswer`, `TariffChangeNotification`), unit-tested, because no test profile starts
  an Enterprise installation next to an editing service on its trial. What stays untested is the wiring that reads
  their inputs.
- **A trial that would already be over is no trial.** The wizard and the anonymous question take the editing
  service's trial only when the end it leads to (the earlier of the installation's end and the editing service's)
  is today or later; otherwise a file is required, as before the change, and the wizard writes nothing. The re-read
  still writes that end. The anonymous question reads the installation's tariff through the cache of the tariff
  service: a miss reads the database (and creates the 30-day trial when there is no tariff, as any first read does),
  and the cached end can lag behind the stored one, until the cache expires after a write by another process without
  Redis, or when a read that started before a write refills the cache after it. The wizard checks the stored tariff
  again before it writes.
- **The answer lives in the distributed cache with fail-safe.** It is one value per installation: the background
  re-read in the worker refreshes it, the API nodes read it. With Redis, the default `IFusionCache` (memory, Redis
  and the backplane) shares it with every node and keeps it over a restart; the `"memory"` cache would keep it in
  one process. An answer is reused for 15 minutes, the last one kept for 3 days while the editing service fails, and
  asked again once a minute; the license command keeps its own 5-second limit. The period of the re-read is at most
  a third of those 3 days (a minute to a day, one day by default), so the last answer outlives one re-read that finds the
  editing service silent. The readers of one process wait for the one request in flight as long as it may run (10
  seconds), not the 5-second lock default of the cache, and a failure with no answer known is stored before they go
  on, so a silent service gets one request per process at a time. A fresh question with a known answer
  replaces the cached value for every node; an unknown one leaves it alone, and so does a failure of a reader with
  no answer to fall back on when another process has stored one meanwhile.
  Alternative: no cache. Rejected: an anonymous endpoint would send one request to the editing service per call.
  Alternative: a setting in the database. Rejected for now: the cache with a 3-day fail-safe covers the restart,
  and a lost answer only makes the wizard fall back to the installation's own trial.
- **A tariff write tells the open clients.** Every write of the tariff (a saved change, a scheduled quantity, a
  deleted tariff) sends `s:change-tariff` to the quota room of the portal, or of every active portal for the shared
  tariff of tenant -1, so the trial banner and the payment page read the new end. The event never reads the tariff:
  a write may run inside the read of the tariff, under its lock, and a read there would wait on that lock. It takes
  the state of the tariff just written, calculated on a copy, which only picks the queue of the socket service. The
  quota values of the write (storage, paid users, rooms, the free flag when it changed) go to the same portals the
  same way, never through the current tenant, which a background write has none of; they are built apart from the
  tariff event, so a quota that cannot be read leaves that event in place. A failed send is logged once per
  write, and a portal that cannot be told does not keep the others from being told. A tariff that becomes not paid
  only because its day passed is not a write and sends nothing.
- **One lock for the license writers.** The trial write and the license-file write take the same distributed lock,
  and the trial checks the license file and the license quota under it. Across processes this holds only with a
  distributed lock provider (Redis or ZooKeeper); without one the lock is per process, and the check under it only
  narrows the race between the API and the worker.
- **Only the trial of the first tariff read is followed.** The re-read follows the `start_trial` quota the tariff
  service creates on the first read of the tariff. A trial activated on request is left as it is, and before the
  first tariff read there is nothing to follow yet, so such a pass changes nothing. The wizard, on an active trial of
  the editing service, writes the `start_trial` quota itself, replacing a trial activated on request.
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
  or the re-read replaces it for every node; while the editing service fails, its last answer of the past 3 days is
  used.
- [Until the wizard is completed during the editing service's active trial, or the first re-read runs, the
  installation's trial keeps its 30-day end, later than the editing service's] → Accepted: the next daily re-read
  brings it closer.
- [Without Redis there is no shared cache] → Each process then keeps its own copy, as the rest of the server's
  caches do: the answer of the worker does not reach the API, and a restart loses the last answer. The re-read and
  the wizard still ask the editing service themselves.
- [A trial write that started before an accept finishes after it] → The shared lock orders them, so the accept ends
  with the license in force, across processes only with a distributed lock provider (Redis or ZooKeeper); without one
  the lock is per process, and the check of the license file under it only narrows the race.

## Migration Plan

No data migration. Existing data stays consistent with the modified requirements:

- An installation with a license from a file keeps it: the trial never replaces the license quota.
- An installation on its own trial keeps that quota; only its tariff end can move closer on the next re-read.
- Rollback: the previous server version reads the same quota and tariff rows; a trial end moved closer stays as it
  is until a license is accepted.
