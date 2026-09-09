---
paths:
  - "products/*/Tests/**/*.cs"
  - "common/Tests/**/*.cs"
---

# Integration Tests

Applies to `products/*/Tests/` and `common/Tests/ASC.Core.Common.Tests`. Style rules
(`csharp-style.md`) apply here too — including the mandatory `dotnet format style` check
after editing any `.cs` file.

## The shared harness lives in `common/Tests/ASC.Tests.Common`

Everything that is not specific to one product is there, and every suite references it:

| Type | Role |
|---|---|
| `AspireHostFixture<TClients>` | starts the Aspire host once per assembly, registers portals, disables start docs, warms up |
| `PortalClientsBase` / `PortalContext` | the per-portal HTTP clients, `Origin` binding, disposal |
| `Initializer` | owner credentials, client-side password hashing, `HttpClient.Authenticate(user)`, `FakerMember` |
| `RawApiClient` / `RawApiResponse<T>` | raw JSON access for endpoints the SDK does not expose |
| `User`, `Timing`, `RegisterPortalModel` … | the small shared data types |

A suite adds only its own two files: an `AspireAppFixture` (naming the Aspire resources it needs in
`Resources`, building its bundle in `CreateClients`, touching its hot path in `WarmUpAsync`) and a
`PortalClients` deriving from `PortalClientsBase`, which calls `CreateClient(ResourceNames.X)` per
service and wires the typed SDK clients onto it. ApiSystem and Web.Api are always started — portal
registration and authentication — so `WebApiHttpClient` and `WebApi` come from the base.

Do not copy any of this back into a suite. If a suite needs something the base does not do, extend
the base.

## The SDK is the source of truth for signatures

Tests call the API through the generated `DocSpace.API.SDK` (pinned in
`Directory.Packages.props`, currently 3.7.0). **Read its source from the repo, never guess a
signature and never dig through the NuGet cache.** Paths are relative to the repo root
(`server/`):

| What | Path |
|---|---|
| .NET SDK — endpoints | `sdk/docspace-api-sdk-csharp/src/DocSpace.API.SDK/Api/<Area>/<Name>Api.cs` |
| .NET SDK — models | `sdk/docspace-api-sdk-csharp/src/DocSpace.API.SDK/Model/<Name>.cs` |
| .NET SDK — model docs | `sdk/docspace-api-sdk-csharp/docs/<Name>.md` (quick property list) |
| TypeScript SDK | `sdk/docspace-api-sdk-typescript/api/` |
| TypeScript test suite | `../tests/api-tests/src/tests/` (sibling of `server/`) |

Both SDKs are generated from the same OpenAPI document, so the TypeScript suite and these
tests expose the same surface and TS tests translate almost one-to-one. When a translation
is unclear, compare the two generated clients side by side.

Two consequences worth remembering:

- Most calls return `ApiResponse<T>` with `.Response`; some (`ResendEmailInvitationsAsync`,
  `TerminateRoomIndexExportAsync`) return plain `Task` — there is no status code to assert,
  so a positive test just awaits the call.
- Parameters named after C# keywords are escaped: `@private`, `@public`, `@internal`.

## One portal per test

`BaseTest.InitializeAsync` registers a brand-new portal and binds a fresh `PortalClients`
bundle to it, which is what makes test classes safe to run in parallel. Never share state
between tests; never assume a portal is empty of other tests' data — it is already yours
alone.

The Aspire host starts once per assembly (`TestAssemblyFixture` → `AspireAppFixture`).
Portals are **not** deleted afterwards.

## Roles: invite, then authenticate

There is no per-role client. Switch identity by re-authenticating the shared client:

```csharp
var user = await InviteContact(EmployeeType.User);
await _filesClient.Authenticate(user);       // act as the user
await _filesClient.Authenticate(Owner);      // back to the owner
await _filesClient.Authenticate(null);       // anonymous
```

`_filesClient`, `_peopleClient` and `_webApiClient` carry independent auth headers — a test
can be the owner on one and a member on another.

**Guests cannot be created with `InviteContact`.** Use `BaseTest.InviteGuest`, which goes
through `POST /api/2.0/people/active` and returns an activated guest with a known password,
so tests can also sign in as it. When a theory is parameterised by `EmployeeType`, route it
through a dispatcher rather than branching at every call site (see
`RoomsPermissionsTestBase.InviteMember`).

## Asserting failures

The SDK throws `ApiException` on any non-2xx. No helper is needed:

```csharp
var exception = await Assert.ThrowsAsync<ApiException>(
    async () => await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

exception.ErrorCode.Should().Be(403);
exception.ErrorContent?.ToString().Should().Contain("Access denied");
```

`ErrorCode` is the HTTP status. Always pass `TestContext.Current.CancellationToken`.

## Endpoints the SDK does not expose

Controller actions marked `[ApiExplorerSettings(IgnoreApi = true)]` are absent from the
OpenAPI document and therefore from the SDK. Call them over raw HTTP through the matching
`HttpClient` (`_peopleClient`, `_filesClient`, …). Same for deliberately malformed bodies,
which a typed DTO cannot express — see `RoomsPermissionsTestBase.SendRawTagsDelete`.

**Raw HTTP is the exception, never the convenient alternative.** Anything the SDK can express
goes through the SDK, including every negative case: a raw request bypasses the generated
client that real callers use, so it keeps passing after the OpenAPI contract breaks — which is
one of the things these tests exist to catch. Once a raw helper exists in a suite it is tempting
to route everything through it; don't. The carve-outs are narrow:

- a body the DTO cannot carry — malformed JSON, a missing body, a wrong-typed value
  (`"rooms":["1"]`, a null array element, a float where an int is required);
- a value a required non-nullable constructor parameter rejects client-side, so no request is
  ever sent;
- a route or query the typed signature cannot produce — a non-integer id where the parameter is
  `int`, `?includeMembers=abc` where it is `bool?`;
- transport-level cases: an unsupported verb, a wrong `Content-Type`.

Note what is *not* on that list: string length, blank or unknown string values, and null on a
nullable property. The DTO accepts all of those and the server does the rejecting, so they are
ordinary typed calls.

One more carve-out, on the response side: a generated model can be too narrow to carry what the
endpoint returns. `FolderContentDtoInteger.Folders` is typed `List<FileEntryBaseDto>`, which has
`Title` but neither `Id` nor `Logo`, so a room read through a folder listing loses both. When the
assertion needs a field the model drops, read the raw JSON — and say so in a comment, because
that is an SDK defect worth reporting, not a preference.

Decide these by evidence, not by feel: read the `[DataMember(..., EmitDefaultValue = ...)]`
attributes on the generated model and the constructor, which together determine what an
all-default instance actually puts on the wire. `EmitDefaultValue = true` means a default
instance serialises to `{"icon":null}`, not `{}` — so those two requests are genuinely different
and only the raw one can send the second.

## Anything written asynchronously has to be polled

"New item" badges, background file operations, template creation and index updates are all
written after the request that triggered them returns. A bare read right after the change races
with the write and fails intermittently — or, worse, passes until the machine is busy.

Poll on a **deadline**, and let the loop end by returning the last observed state:

```csharp
var deadline = DateTime.UtcNow.AddSeconds(10);

while (true)
{
    var titles = TitlesOf((await _roomsApi.GetNewRoomItemsAsync(roomId, TestContext.Current.CancellationToken)).Response);

    if (until(titles) || DateTime.UtcNow >= deadline)
    {
        return titles;
    }

    await Task.Delay(500, TestContext.Current.CancellationToken);
}
```

Do **not** pass a timeout `CancellationToken` to the API call inside the loop. When it fires, the
test dies with `TaskCanceledException : A task was canceled` instead of an assertion that shows
what was actually there — which turns a two-minute diagnosis into a twenty-minute one.

## Access-level matrices must match the product

`FileSecurity.AvailableRoomAccesses`
(`products/ASC.Files/Core/Core/Security/FileSecurity.cs`) is the authority on which
`FileShare` values an invitation accepts, per room type and subject type. Inviting outside
that table fails in Arrange and the test is simply wrong. For `SubjectType.User`:

| Room | Allowed |
|---|---|
| CustomRoom | RoomManager, ContentCreator, Editing, Review, Comment, Read |
| PublicRoom | RoomManager, ContentCreator |
| FillingFormsRoom | RoomManager, ContentCreator, FillForms |
| EditingRoom | RoomManager, ContentCreator, Editing, Read |
| VirtualDataRoom | RoomManager, ContentCreator, Editing, Read, FillForms |
| AiRoom | RoomManager, ContentCreator, Read |

Plus one rule that is not in that table: **`RoomManager` can only be granted to a
`RoomAdmin`** — the API rejects it for a `User` or a `Guest`.

Shared matrices live in `RoomAccessData`; check the table before adding a new one, and read
it again when a matrix is reused against a different room type. Read it from the source file
every time — it changes, and a copy of it kept anywhere else goes stale silently.

## One feature, one folder

Every feature gets its own subfolder holding **all** of its suites — functional, validation and
permission alike. Do not scatter one feature across sibling folders, and do not collect
unrelated features into a shared `Permissions/` bucket.

```
Tests/03_Rooms/
├── Covers/                       ← the covers feature, end to end
│   ├── RoomCoverGalleryTests.cs
│   ├── RoomCoverChangeTests.cs
│   ├── RoomCoverValidationTests.cs
│   └── RoomCoverPermissionsTests.cs
├── FormFilling/
├── Permissions/                  ← permissions of the rooms feature itself
├── RoomsPermissionsTestBase.cs   ← shared by the subfolders, so it sits above them
├── RoomAccessData.cs
└── RoomsApiTests.cs
```

The namespace follows the folder (`ASC.Files.Tests.Tests._03_Rooms.Covers`), as everywhere else
in the repo. Keep anything shared by several feature folders **one level up**: a child namespace
resolves its parent without a `using`, which matters here because usings are only allowed in
`GlobalUsings.cs`.

## Class size drives parallelism

xUnit parallelises **test collections**, and by default a collection is a class. Cases
inside one class always run sequentially — there is no setting that changes this. A
`[Theory]` with 16 cases is 16 sequential tests.

So: **one class per endpoint, and keep it under ~24 cases** (cases, not methods — count
`InlineData` rows and `MemberData` sizes). Split by endpoint or by scenario group, put
shared helpers in an intermediate base class, and shared `TheoryData` in a static data
class referenced explicitly:

```csharp
[MemberData(nameof(RoomAccessData.InvitedMemberAccesses), MemberType = typeof(RoomAccessData))]
```

Reference layout: `products/ASC.Files/Tests/Tests/03_Rooms/Permissions/`.

## Known bugs

A test that covers a bug gets `[Trait("Bug", "12345")]`. Do not skip it and do not assert the
buggy behaviour — assert what the product is supposed to do, so the test is red while the bug
is open and turns green when it is fixed. When porting from the TypeScript suite,
`test.fail(...)` marks exactly these.

**The trait stays after the fix.** It is the permanent link between the test and the bug
record, which is what makes a regression immediately attributable: if that test goes red
again, the bug number is right there. Only the `<summary>` changes — describe the old
behaviour in the past tense and say how it was fixed.

## Running

```bash
dotnet test ASC.Tests.slnx
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-class "*RoomCreate*"
```

Filters use **Microsoft.Testing.Platform** syntax and go after `--`: `--filter-class`,
`--filter-method`. VSTest's `--filter Name~...` does not apply.

Note that Rider does **not** use MTP for xUnit — it runs its own ReSharper test runner, so
timings and parallelism there differ from `dotnet test`. Anything that must hold in both
belongs in code (e.g. `[assembly: CollectionBehavior(...)]`), not on the command line.
