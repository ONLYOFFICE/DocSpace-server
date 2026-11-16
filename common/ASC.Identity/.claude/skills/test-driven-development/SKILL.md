---
name: test-driven-development
description: >-
  Test-driven development for this Java/Maven/Spring Boot repo. Use when
  implementing any feature or bugfix, before writing production code.
  Enforces red-green-refactor with JUnit 5 + AssertJ and Given-When-Then
  test topology (// given, // when, // then).
---

# Test-Driven Development (TDD)

Adapted from [obra/superpowers](https://github.com/obra/superpowers) (`test-driven-development`). See [LICENSE](LICENSE) (MIT).

## Overview

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

**Violating the letter of the rules is violating the spirit of the rules.**

## When to Use

**Always:**
- New features
- Bug fixes
- Refactoring
- Behavior changes

**Exceptions (ask your human partner):**
- Throwaway prototypes
- Generated code
- Configuration files

Thinking "skip TDD just this once"? Stop. That's rationalization.

## The Iron Law

```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```

Write code before the test? Delete it. Start over.

**No exceptions:**
- Don't keep it as "reference"
- Don't "adapt" it while writing tests
- Don't look at it
- Delete means delete

Implement fresh from tests. Period.

## Red-Green-Refactor

### RED — Write Failing Test

Write one minimal test showing what should happen. **Every test body must use Given-When-Then topology** (see below).

```java
@Test
void retriesFailedOperationsThreeTimes() {
  // given
  AtomicInteger attempts = new AtomicInteger();
  Supplier<String> operation = () -> {
    if (attempts.incrementAndGet() < 3) {
      throw new IllegalStateException("fail");
    }
    return "success";
  };

  // when
  String result = RetryOperation.execute(operation);

  // then
  assertThat(result).isEqualTo("success");
  assertThat(attempts.get()).isEqualTo(3);
}
```

Clear name, real behavior, one thing.

**Bad:** vague name (`retryWorks`), testing mocks instead of behavior, missing `// given` / `// when` / `// then`.

**Requirements:**
- One behavior
- Clear name
- Real code (no mocks unless unavoidable)
- Given-When-Then comment sections

## Given-When-Then topology (mandatory)

Every test method body is structured as three labeled blocks:

| Section | Comment | Contains |
|---------|---------|----------|
| **Given** | `// given` | Fixtures, stubs/mocks, preconditions |
| **When** | `// when` | Single action under test (one call / one interaction) |
| **Then** | `// then` | Assertions and verifies only |

```java
@Test
void rejectsEmptyEmail() {
  // given
  var form = new FormData("");

  // when
  var result = FormSubmitter.submit(form);

  // then
  assertThat(result.error()).isEqualTo("Email required");
}
```

**Exception path** — keep Given separate; combine When+Then only when the action is the assertion:

```java
@Test
void throwsWhenEmailAlreadyExists() {
  // given
  when(userRepository.existsByEmail("a@b.com")).thenReturn(true);

  // when / then
  assertThatThrownBy(() -> userService.create(request("a@b.com")))
      .isInstanceOf(DuplicateResourceException.class);

  verify(userRepository, never()).save(any());
}
```

**Rules:**
- Lowercase comments: `// given`, `// when`, `// then` (or `// when / then` for throw assertions)
- No assertions in Given; no setup after When
- One When action per test — split tests instead of multiple whens
- Arrange-Act-Assert is the same idea; **use the GWT labels**, not `// arrange`

### Verify RED — Watch It Fail

**MANDATORY. Never skip.**

```bash
mvn -pl <module> -Dtest=RetryOperationTest#retriesFailedOperationsThreeTimes test
```

Confirm:
- Test fails (not errors)
- Failure message is expected
- Fails because feature missing (not typos)

**Test passes?** You're testing existing behavior. Fix test.

**Test errors?** Fix error, re-run until it fails correctly.

### GREEN — Minimal Code

Write simplest code to pass the test. No extra options, no YAGNI features.

### Verify GREEN — Watch It Pass

```bash
mvn -pl <module> -Dtest=RetryOperationTest test
```

Confirm:
- Test passes
- Other tests still pass
- Output pristine (no errors, warnings)

**Test fails?** Fix code, not test.

### REFACTOR — Clean Up

After green only: remove duplication, improve names, extract helpers. Keep tests green. Don't add behavior.

## Project conventions

- **Given-When-Then topology** on every test (`// given` / `// when` / `// then`)
- JUnit 5 + AssertJ (prefer `assertThat`)
- Positive **and** negative cases (per project rules)
- Prefer testing ports/adapters with focused unit tests; use `@SpringBootTest` only when wiring matters
- Test names follow `givenX_whenY_thenZ`
- Module-scoped Maven: `-pl authorization/authorization-application -am` (minified: `-Pminified -pl minified`); don't rebuild the world for one test
- Integration tests (`*IT`) need Docker and `-DRUN_INTEGRATION_TESTS=true`; run them after `mvn clean`
- Production code uses Lombok; tests build fixtures with the Lombok builders of the request DTOs

When writing or changing any test, read [writing-good-tests.md](writing-good-tests.md):
- Name the production change that would make the test fail — before writing it
- Assert on real behavior, never on mock behavior
- Keep test-only code in test utilities, out of production classes
- Understand a dependency's side effects before mocking it

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests after pass immediately — proves nothing about catching bugs. |
| "Already manually tested" | No durable regression suite. |
| "Deleting X hours is wasteful" | Sunk cost. Rewrite with TDD or keep untrusted code. |
| "Keep as reference, write tests first" | You'll adapt it. That's testing after. Delete means delete. |
| "Need to explore first" | Fine. Throw away exploration, start with TDD. |
| "Test hard = design unclear" | Listen to the test. Hard to test = hard to use. |
| "TDD will slow me down" | Catching bugs before commit is faster than production debugging. |

## Red Flags — STOP and Start Over

- Code before test
- Test after implementation
- Test passes immediately
- Can't explain why test failed
- Tests added "later"
- Rationalizing "just this once"

**All of these mean: Delete code. Start over with TDD.**

## Example: Bug Fix

**Bug:** Empty email accepted

**RED**
```java
@Test
void rejectsEmptyEmail() {
  // given
  var form = new FormData("");

  // when
  var result = FormSubmitter.submit(form);

  // then
  assertThat(result.error()).isEqualTo("Email required");
}
```

**Verify RED** — failure shows missing validation.

**GREEN** — add the null/blank check only.

**Verify GREEN** — pass.

**REFACTOR** — extract shared validators if needed.

## Verification Checklist

Before marking work complete:

- [ ] Every new function/method has a test
- [ ] Each test uses `// given` / `// when` / `// then` (or `// when / then`)
- [ ] Watched each test fail before implementing
- [ ] Each test failed for expected reason (feature missing, not typo)
- [ ] Wrote minimal code to pass each test
- [ ] All tests pass
- [ ] Output pristine (no errors, warnings)
- [ ] Tests use real code (mocks only if unavoidable)
- [ ] Edge cases and errors covered

Can't check all boxes? You skipped TDD. Start over.

## Debugging Integration

Bug found? Write failing test reproducing it. Follow TDD cycle. Use `systematic-debugging` for root cause first.

Never fix bugs without a test.

## Final Rule

```
Production code → test exists and failed first
Otherwise → not TDD
```

No exceptions without your human partner's permission.
