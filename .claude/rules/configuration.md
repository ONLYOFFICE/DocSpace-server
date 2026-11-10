---
paths:
  - "**/*.cs"
---

# Configuration values

A configuration value is written by an administrator on a host you never see: a typo, a number in
the wrong unit, a zero meant as "off". The development stand runs on the defaults, so a bad value
first shows up at a customer's. Reviews found three outcomes: the host does not start, a protection
switches off without a word in the log, or a limit limits nothing. Reviewers check the same points
as matrix M3 of `review-branch` and cite them as `[configuration.md §N]`.

## 1. Read through the helper

- Read numbers, intervals and flags with `ConfigurationValueExtensions`
  (`common/ASC.Common/Utils/ConfigurationValueExtensions.cs`): `ReadInterval`, `ReadInt`,
  `ReadLong`, `ReadBool`. Never use `TimeSpan.Parse`, `int.Parse`, `GetValue<T>`, or `Get<T>()` of a
  section without a catch.
- A missing key gives the default. An unreadable value gives the default and a warning. A value out
  of bounds gives the nearest bound and a warning. The helper never throws for what the
  configuration holds. `ReadBool` also takes `1`/`0`, `yes`/`no`, `on`/`off`.
- When a section is bound into a class (`Bind`, `Get<T>()`), the binding throws on a bad value.
  Catch it and fall back to the defaults with a warning, then check the bound fields against the
  same bounds.
- This rule does not ask for existing reads to be rewritten. A read you add or change uses the
  helper.

## 2. An optional value never stops the host

- An optional feature or a tuning knob never prevents the host from starting, least of all when
  the feature itself is off. Seen in review: `period: "1d"` failed the binding during service
  registration, and the whole monolith did not start although the cleanup was disabled.
- Only what the service cannot run without (a connection string, an encryption key) may fail at
  startup, with a message that names the key.

## 3. Both bounds, chosen from what the value drives

- The minimum keeps a loop from running hot (a period of minutes, not of milliseconds), and keeps a
  destructive interval above what it must outlive (an idle interval above the touch interval).
- The maximum keeps arithmetic from overflowing (`AddDays` with a million days threw before the
  loop started), and keeps a protective limit finite.
- Write the unit, the bounds and an example in the comment at the read. `TimeSpan` reads a bare
  number as days: `"60"` is 60 days.

## 4. Zero, negative, off

- If 0 means "off", say so at the read and log once at startup that the feature is off by
  configuration. A negative value never switches anything off by accident: it falls below the minimum.
- Prefer a separate `enabled` flag (`ReadBool`) to a zero in a number.
- One key does one thing. A key that switches off writing a mark must not also switch off reading it.
- Seen in review: `validinterval ≤ 0` turned the repeat check off, and `enabled=1` left the cleanup
  unregistered, both with nothing in the log.

## 5. The safe default lives in the code

- A limit that protects the host (an extraction ceiling, a batch size, a retention period) has a
  finite default in the code. A value set only in `ProjectConfigurator` or in `buildtools/config` is
  not a default: a standalone, docker or customer configuration may not have it.
- Seen in review: `migration:maxExtractedSize` defaulted to `long.MaxValue`, and the ceiling existed
  only in the `integration-test` profile, so the zip-bomb guard worked only in the tests.

## 6. Related values are checked together

- Examples: an idle interval above the touch interval, a lease longer than the longest batch, a
  retry budget within the request timeout. Encode the relation in the bounds (the `min:` of one
  taken from the other), or check it after reading both and fall back with a warning.
- `Math.Min(a, b)` over two settings skips the one that is ≤ 0. It must not become ≤ 0 itself.

## 7. Read once

- Read and check at startup or when the service is created, and keep the result in a field. Never
  read `configuration["..."]` on every request or every pass: the warning is then written every
  time, and the value can change under a running loop.

## 8. Guard

- The helper's own table of values is `ConfigurationValueExtensionsTests`. A value the helper does not cover alone
  (a relation from §6, a bound section, a combination) gets a unit test of its own. The table: key
  missing, unreadable, 0, negative, above the maximum. Each case gives the documented value and,
  except for the missing key, a warning.
- When the value is read during service registration, add a test that the services still register
  with a broken section.
