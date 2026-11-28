# A library's behaviour is settled by a test

A decision or a text that rests on how a library behaves — the FusionCache fail-safe and
`context.Fail`, factory soft and hard timeouts, `ExpireAsync` on the distributed level, what a Polly
circuit breaker counts, a retry predicate, the order `ConfigureHttpClientDefaults` applies in — rests
on a guess until a test shows the behaviour. The documentation describes the common case; the code
under review usually sits in a corner of it (an expired local entry next to a stale distributed one,
a factory past its soft timeout). This rule has no `paths:`: a text in a spec or a commit message
can rest on such behaviour as well as code. Reviewers cite it as `[dependency-behaviour.md §N]`.

## Checklist

- Each decision or text that rests on a library's behaviour has a characterization test in the same change (§1)
- That test runs the real library with the options the code uses, not a mock, and is named for the behaviour (§1)
- A reviewer's question about such behaviour is answered by that test, not by reasoning or a disclosure (§2)
- For a cache, each cell of matrix M13 names the characterization test it rests on (§3)

## 1. A characterization test in the same change

- The change that starts relying on such behaviour adds a unit test of the behaviour itself: the
  real library with the options the code uses (the same entry options, the same pipeline builder),
  not a mock of it. The test is named for the behaviour and fails when the library does otherwise.
- The same holds for a fix round that writes a sentence about such behaviour (`claims.md` §6).

## 2. A question about it is answered by a test

- A reviewer's question about such behaviour ("not verified from source", "does `context.Fail` keep
  the stale value?") is answered in the fix round by the characterization test, whichever way it
  comes out. Not by reasoning, not by reading the library's source alone, and not by a sentence that
  discloses the doubt in a remark or the PR description. A review does not close such a question
  without the test (`review-branch` §4 "Disclosed is not decided").
- Seen in review, in one branch, each assumed for several rounds and settled only by a test: `context.Fail`
  re-armed the fail-safe, so a known "available" never ended; on the 200 ms soft timeout the cache
  answered the stale value past the factory's own check of its age; with an expired local entry and a
  stale distributed one, the factory saw the distributed `true` but `context.Fail` returned the local
  `false`; `ExpireAsync` removes the distributed entry.

## 3. Guard

- `review-branch` matrix M13 (cache answer paths): each cell names the characterization test it
  rests on; a cell without one is a finding.
