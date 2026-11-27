# ai-agents — verification

> **Test run 80268dcdbe** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-14), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario group of `spec.md`, with where it was verified. Paths are relative to the repo root
(`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml` at submodule commit `5ea4dc6` (line numbers are
of that commit; the working tree of the submodule is at `baf28cf`, which adds private-room and skills-generation
operations and does not change the contract verdicts below). Services: `Chat` is the Node AI chat service
`common/ASC.AI.Chat/app/` (public `/api/2.0/ai/*`), `AI` is the DocSpace AI service `products/ASC.AI/` (`Server/Api/`,
`Server/Service/` on `/internal/ai/*` and the settings, vectorization and export routes), `Lib` is the chat library
`common/ASC.AI.Chat/node_modules/@onlyoffice/ai-chat/` (1.0.2-progress.37), `Security` is
`products/ASC.Files/Core/Core/Security/FileSecurity.cs`, `Payments` is `web/ASC.Web.Api/Api/PaymentsController.cs`.
Tests: `T/` is `products/ASC.AI/ASC.AI.Tests/Tests/`, `Hub/` is `products/ASC.Files/Tests/Tests/03_Rooms/AiHub/`,
`Instr/` is `products/ASC.Files/Tests/Tests/03_Rooms/Instructions/`, `Web/` is `web/ASC.Web.Api.Tests/Tests/`,
`CoreTests/` is `common/Tests/ASC.Core.Common.Tests/`, `TS` is `../tests/api-tests/src/tests/ai/`; `file:line` is the
line of the test method.

Status: **TEST-CONFIRMED** (a .NET test of this repository asserts it), **TS-ONLY** (no .NET test; only the TypeScript
suite, which runs against a billing and gateway sandbox, asserts it — a claim, not proof), **CODE-ONLY** (read from
the product code or the OpenAPI document). A suffix `(skips on the stand)` means the .NET test exists but skips on the
standalone integration host: editor generators need the `automationapi` quota (`T/EditorToolTests/GeneratorQuota.cs:53`),
REST editor tools need the docspace-mcp container (`T/EditorToolTests/EditorToolTestBase.cs:329`), third-party tests need
Nextcloud (`T/ThirdPartyTests/ThirdPartyTestBase.cs:68`). A suffix `(TS block skipped)` means the TS block is now
`test.skip(true, "AI agents removed in DocSpace 4.1.0")`, so the claim was last measured before 4.1.

Exception → status mapping of the .NET services: `SecurityException` and `InvalidOperationException` 403,
`ArgumentException` 400, `ItemNotFoundException` 404, `TenantQuotaException` 402
(`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`); Chat relays the AI service's status (`AiServiceHttpError`) and adds
its own 400s in the controllers. Status of a defect marked "red by code reading": no test was run in this revision.

Revision 2026-11-18 (c8bb15c779, no product change): line citations that no longer pointed at the code their row describes were moved to it (most shifted with the longer controller remarks); no row status or claim changed, except where a quoted remark or a removed attribute is noted in the row.

Revision 2026-11-14 (09d4de0768..6fb6dfb17a): document agent portal settings in the AI service (`d5d515f748`) proxied by the chat service (`d4463d237b`, `33789fe142`, `3400e63edb`), the task log kept from the chat model (`a7d94e4b81`), `POST skills/create` (`051a7422c3`); the spec now names every `aiPrompts*` operation. New defect A35. Rows added carry no run mark; "unit level" rows cite vitest files that were not run in this revision.

Revision 2026-11-09 (bb406b409b..6fb013e745): BYOK for AI (keys, providers, the gateway BYOK route, the revert of embedding and web-search BYOK); see the section of that name.

Revision 2026-11-06: re-checked against the merge of `release/v4.1.0` (`d58816bece..3533ad8a89`). The range removed the
agent routes (`ecf76402b3`), added Instructions and Knowledge rooms (`8fe15a629c`), AI projects, sources and their model
(`1c4190d327`, `ee2f480944`, `96a4fdefca`, `e10a553243`, `64c79db1dc`), the skills catalogue (`69e51f8e97`) and the
multi-room knowledge search (`10d18364af`, `3878ba506a`). The TS AI suite was ported to .NET (2300 cases). How rows were
checked: the assertion was read for the Category=Bug tests, the tests behind the defects of that revision and the suspicious rows (about 40
tests); the other `file:line` citations come from the test index and its summaries, by test name. The spot checks found
two rows of the previous draft wrong (`0` as a preference scope, `ask` and a saved approval) and the name of
`Add_NameOfARegisteredServerWithoutConfig` read against the draft's "re-save"; a name may still mislead elsewhere.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Portal AI switch** | | |
| Read by every member, on by default | `Web/02_Settings/Common/AiAccessGetTests.cs:46,65`; `common/ASC.Core.Common/Tenants/TenantAiAccessSettings.cs:9-12` | TEST-CONFIRMED [run: PASS] |
| Written by owner / DocSpace admin; others 403; anonymous 401; scenarios "turns AI off for everyone", "Room admin cannot flip" | `T/SettingsTests/SettingsAiAccessTests.cs:55,81,99,108` | TEST-CONFIRMED [run: PASS] |
| Every AI route 403 while off, before the target is looked up | `T/ProfileTests/ProfileAiDisabledTests.cs:51,111`; `T/AssignmentTests/AssignmentAiDisabledTests.cs:46`; `T/ChatTests/ChatAiDisabledTests.cs:66,115`; `T/McpTests/McpToolsAiDisabledTests.cs:58`; `T/PromptTests/PromptAiDisabledTests.cs:50`; `T/ThreadStorageTests/ThreadsAiDisabledTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| AI rooms section hidden while off, back on restores (scenarios "AI rooms are hidden", "Switching back on") | `Hub/AiHubAiAccessTests.cs:45-174`; `T/SettingsTests/SettingsAiDisabledTests.cs:110,139` | TEST-CONFIRMED [run: PASS] |
| Audited and pushed to clients | `web/ASC.Web.Api/Api/Settings/SettingsController.cs` (`ChangeAiAccessSettingsAsync`, audit message) | CODE-ONLY |
| **AI readiness reported to the portal** | | |
| Every role incl. Guest, same fields, anonymous 401; read while off; self-hosted `aiReady` needs a profile (scenario "Read while the switch is off") | `T/SettingsTests/SettingsGetTests.cs:77,91,102,133,153`; `T/SettingsTests/SettingsAiDisabledTests.cs:83,124` | TEST-CONFIRMED [run: PASS] |
| Hosted: first three true only while on and paid (scenario "Unpaid hosted portal") | `TS/settings/settings.ai-disabled.spec.ts:174`; `AI/Core/Settings/AiSettingsService.cs` | TS-ONLY |
| **The AI Tools wallet service gates inference only** | | |
| Send and regenerate 402, nothing stored (scenario "Send refused while unpaid") | `TS/chat/chat.ai-disabled.spec.ts:352`; `Chat/controllers/aiController.ts` | TS-ONLY (TS block skipped) |
| Gateway proxy paths 403 while unpaid | `AI/Server/Api/AiGatewayProxyController.cs` (`AiServiceDisabledException`) | CODE-ONLY |
| Management surface stays open (scenario); low balance does not stop inference | `TS/chat/chat.ai-disabled.spec.ts:488,553`; `TS/mcp/mcp.ai-disabled.spec.ts` | TS-ONLY (TS block skipped) |
| **AI tools version of the editors** | | |
| `aiTools` next to the editors' version, null when off or old (scenario) | `web/ASC.Web.Api/Core/BuildVersion.cs:55,83`; `products/ASC.Files/Core/Helpers/DocumentService.cs` | CODE-ONLY |
| **Per-user AI chat settings** | | |
| Own value, true for new user, every role, while off (scenario "One user's preference is private") | `T/SettingsTests/SettingsUserConfigTests.cs:46,64,110,223`; `SettingsUserConfigPermissionsTests.cs:51,70,88`; `SettingsAiDisabledTests.cs:98,139` | TEST-CONFIRMED [run: PASS] |
| Non-boolean, empty, missing, non-object 400; unknown field ignored; 200 means stored (scenario "Non-boolean value") | `T/SettingsTests/SettingsUserConfigBodyTests.cs:79,96,113,125,137,157` | TEST-CONFIRMED [run: PASS] |
| **Tool permission mode of a user** | | |
| Three modes, auto default, both read pairs, other preference leaves it (scenario "Modes round-trip") | `T/McpTests/McpToolPermissionModeSettingsTests.cs:65,76,127`; `T/PreferenceTests/PreferencePermissionTests.cs:162` | TEST-CONFIRMED [run: PASS] |
| Outside the three 400 (scenario "Value outside the three"); per user (scenario "Modes are per user") | `T/McpTests/McpToolPermissionModeSettingsTests.cs:89,105` | TEST-CONFIRMED [run: PASS] |
| Steers the pause; next tool call (scenario "Mode steers the approval pause") | `T/McpTests/McpToolPermissionModeChatTests.cs:91,121,137,153,166,200` | TEST-CONFIRMED [run: PASS] |
| `ask` ignores a saved pre-approval (scenario "Pre-approval in ask") | `Lib/dist/core/services/ai/types.d.ts:503` ("under ask never"); `T/McpTests/McpToolPermissionModeChatTests.cs:111` asserts the opposite and is red by code reading (see differences); run 2026-11-10: `Ask_ASavedAllowAlways_Wins` red with "found False" | TEST-CONFIRMED [run: FAIL-BUG] - the red TS-claim test proves the row: under `ask` the saved grant is ignored (`T/McpTests/McpToolPermissionModeChatTests.cs:111`) |
| **Gates on the tool permission mode routes** | | |
| Guest 403 on the routes (scenario "Guest on the two read routes"); invalid value from Guest 400, not 403 | `T/PreferenceTests/PreferencePermissionTests.cs:198` to line 212; the 400: failure text of `ToolPermissionMode_Guest_CannotReadOrChangeIt` in run 365d87fe60 (bug 84303, A27); run 2026-11-10: the same test red at the invalid-value assertion again | TEST-CONFIRMED [run: FAIL-BUG] - the Guest read 403s hold, the test fails only at the 400 (A27), `T/PreferenceTests/PreferencePermissionTests.cs:198` |
| AI off: invalid value 400, validation first | failure text of `PreferenceAiDisabledTests.ToolPermissionMode_AiDisabled_ReadAndWriteReturn403` in run 365d87fe60 (bug 84302, A27); run 2026-11-10: red again with 400 | TEST-CONFIRMED [run: FAIL-BUG] - `T/PreferenceTests/PreferenceAiDisabledTests.cs:76` (A27) |
| **Profile catalogue** | | |
| Identical description to reading roles, get-by-id has no key or headers | `T/ProfileTests/ProfileCatalogueTests.cs:56,96,133,178`; `ProfileAccessReadTests.cs:91` | TEST-CONFIRMED [run: PASS] |
| Guest 403, anonymous 401, malformed / empty 400, unknown 404 (scenarios "Guest cannot read", "Unknown id") | `T/ProfileTests/ProfileAccessGuestTests.cs:47,58`; `ProfileAccessReadTests.cs:52`; `ProfileIdBoundaryTests.cs:46,54,73` | TEST-CONFIRMED [run: PASS] |
| List carries the key to an administrator (scenario) | `T/ProfileTests/ProfileAccessReadTests.cs:108` | TEST-CONFIRMED [run: PASS] |
| Hosted catalogue: gateway list, no embedding, restricted leaves (scenario "Restricted model leaves") | `TS/profiles/profiles.spec.ts:1477,1513`; `AI/Server/Service/ProfileStorageService.cs` | TS-ONLY |
| **Models offered by a stored profile's provider** | | |
| Missing / malformed 400, unknown 404, every reading role, lists models | `T/ProfileTests/ProfileIdBoundaryTests.cs:85,93,113,128`; `ProfileAccessReadTests.cs:153` | TEST-CONFIRMED [run: PASS] |
| Guest and AI off refused before the dial (scenario) | `T/ProfileTests/ProfileAccessGuestTests.cs:82`; `ProfileAiDisabledTests.cs:73` | TEST-CONFIRMED [run: PASS] |
| Key rejected 400, no answer 502 | yaml `aiProfilesListModels`; `T/ProfileTests/ProfileProviderReachabilityTests.cs:45` (discovery sibling); `T/ProfileTests/ProfileListModelsFailureTests.cs:45,67` | TEST-CONFIRMED [run: FAIL-BUG] for the 400 (`ProfileListModelsFailureTests.cs:45`); for the 502 (`:67`): a provider that drops the connection is answered 200 with an empty list (A32). A refused connection on a saved profile stays CODE-ONLY |
| **Managing profiles** | | |
| DocSpace admin only; others 403; anonymous 401 before validation; AI off first (scenario "Room admin cannot delete") | `T/ProfileTests/ProfileWriteGuardTests.cs:110,132`; `ProfileAccessReadTests.cs:73`; `ProfileAiDisabledTests.cs:152,173`; `T/ProfileStorageTests/ProfileDeleteTests.cs:62,79` | TEST-CONFIRMED [run: PASS] |
| Round trip (scenario "Self-hosted admin round-trips") | `T/ProfileStorageTests/ProfileCreateTests.cs:43`; `ProfileUpdateTests.cs:41`; `ProfileDeleteTests.cs:41`; `T/ProfileTests/ProfileCatalogueTests.cs:56` | TEST-CONFIRMED [run: PASS] |
| Unusable profile refused before connecting (scenario) | `T/ProfileTests/ProfileWriteGuardTests.cs:50,67,84`; `ProfileProviderTypeTests.cs:110,138,152` | TEST-CONFIRMED [run: PASS] |
| Hosted: every write 403 (scenario "Owner on a hosted portal") | `AI/Server/Service/ProfileStorageService.cs` (`AssertGatewayNotConfigured`); TS `profiles/profiles.spec.ts` hosted block | TS-ONLY |
| Base URL ≤ 2048, key ≤ 4096; audited | `AI/Server/Models/RequestDto/AiIntegrationLimits.cs`; `ProfileStorageService.cs` (`AiProfileCreated/Updated/Deleted`); `T/ProfileStorageTests/ProfileFieldLimitTests.cs:57,78` (internal route) | TEST-CONFIRMED [run: PASS] for both limits, boundary and one over (`ProfileFieldLimitTests.cs:57,78`); the audit stays CODE-ONLY (the audit consumer is not in the stack) |
| **Provider discovery** | | |
| Required fields named, empty body, malformed URL, bad key header 400 | `T/ProfileTests/ProfileProviderValidationTests.cs:63,85,104,118,144,164` | TEST-CONFIRMED [run: PASS] |
| Private address, scheme refused before dialling (scenario) | `T/ProfileTests/ProfileProviderUrlGuardTests.cs:56,74,100`; `ProfileProviderReachabilityTests.cs:87` | TEST-CONFIRMED [run: PASS] |
| Wrong key (scenario); 502; no echo, nothing stored | `T/ProfileTests/ProfileProviderModelsTests.cs:134,149`; `ProfileProviderReachabilityTests.cs:45`; `ProfileProviderValidationTests.cs:191` | TEST-CONFIRMED [run: PASS] |
| Local types (scenario "Local provider"), every model named; Guest and AI off | `T/ProfileTests/ProfileProviderReachabilityTests.cs:60,72`; `ProfileProviderModelsTests.cs:104`; `ProfileAccessGuestTests.cs:103`; `ProfileAiDisabledTests.cs:90` | TEST-CONFIRMED [run: PASS] |
| **Connection check of a stored profile** | | |
| Bare id or wrapper; missing / malformed 400; unknown 404; flat verdict (scenario) | `T/ProfileTests/ProfileTestConnectionTests.cs:47,58,74,107,118,128` | TEST-CONFIRMED [run: PASS] |
| Same verdict per role; Guest 403, malformed id before the role check | `T/ProfileTests/ProfileAccessReadTests.cs:153`; `ProfileAccessGuestTests.cs:69,119,136` | TEST-CONFIRMED [run: PASS] |
| **Model restrictions and prices of a hosted portal** | | |
| Null entry 400 before the action (scenario "Null entry"); valid body reaches the 403 of the test portal | `Web/04_Portal/Payments/PaymentAiModelRestrictionsValidationTests.cs:55,73,89,107` | TEST-CONFIRMED [run: PASS] |
| `getAiPrices`, `getRestrictedAiModels`, the rest of `setRestrictedAiModels` | `Payments` (three actions); yaml 53405-53546 at `baf28cf`, `getAiPrices` etc. at 5ea4dc6 | CODE-ONLY |
| Restriction reaches an open conversation (scenario) | `TS/messages/messages.spec.ts`, `TS/agents/agents.spec.ts` restriction blocks | TS-ONLY (TS block skipped) |
| **Tasks and the scope of the assignments** | | |
| The eleven tasks | `common/ASC.AI.Integration/Assignments/ActionType.cs`; yaml 69851 (`GenerateMeta`); `T/AssignmentTests/AssignmentAssignTests.cs:100` | TEST-CONFIRMED [run: PASS] |
| Unknown scope 404 (scenario); unopenable 403; Room is the portal map (scenario) | `T/AssignmentTests/AssignmentEntityScopeTests.cs:51,67,81`; `T/ChatTests/ChatRoomModelTests.cs:46,122`; `T/AssignmentsStorageTests/AssignmentsReadTests.cs:108,123` | TEST-CONFIRMED [run: PASS] |
| Unknown task 400 | `T/AssignmentTests/AssignmentReadTests.cs:85`; `AssignmentRemoveTests.cs:61` | TEST-CONFIRMED [run: PASS] |
| **Who may read and write assignments** | | |
| Reads for three roles, Guest 403, anonymous 401, AI off 403 (scenario "Guest cannot read") | `T/AssignmentTests/AssignmentPermissionTests.cs:46,80`; `AssignmentAiDisabledTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Writes DocSpace-admin only; refused writes nothing (scenario "Room admin cannot bind") | `T/AssignmentTests/AssignmentPermissionTests.cs:134`; `AssignmentAiDisabledTests.cs:73` | TEST-CONFIRMED [run: PASS] |
| **Resolving the model of a task** | | |
| Own binding, then Default; FormAnalysis independent (scenario "Unassigned falls back") | `T/AssignmentTests/AssignmentReadTests.cs:68`; `AssignmentResolutionTests.cs:51,72,87,109` | TEST-CONFIRMED [run: PASS] |
| 404 not 500; try-resolve 200 null; image default (scenarios "Default holds an image model", "Chat and a profile that cannot chat") | `T/AssignmentTests/AssignmentResolutionTests.cs:121,139,153,175` (84450 asserts not-500; the 404 is Chat `assignmentsController.ts:131-151`) | TEST-CONFIRMED [run: PASS] |
| `expand` `profile` with totals | `T/AssignmentTests/AssignmentEntityScopeTests.cs:91` | TEST-CONFIRMED [run: PASS] |
| Hosted defaults (scenario) | `TS/assignments/assignments.spec.ts:33` (two bindings); `AI/Server/Service/AssignmentsResolver.cs:11-32` (FormAnalysis) | TS-ONLY |
| **Writing assignments** | | |
| Capability check; unknown profile refused (scenario); missing field 400 | `T/AssignmentTests/AssignmentCapabilityTests.cs:64`; `AssignmentAssignTests.cs:151,169` | TEST-CONFIRMED [run: PASS] |
| Bulk all or nothing (scenario), empty map (scenario) | `T/AssignmentTests/AssignmentBulkTests.cs:42,62,87,99,122,149` | TEST-CONFIRMED [run: PASS] |
| Unassign, cascade | `T/AssignmentTests/AssignmentRemoveTests.cs:42,61,75,102` | TEST-CONFIRMED [run: PASS] |
| Audited (scenario) | `AI/Server/Service/AssignmentsStorageService.cs` (`AiProfileAssigned`, `AiProfileUnassigned`) | CODE-ONLY |
| **The AI rooms section** | | |
| Created by owner / admin / Room admin, User 403; section root (scenario "Section listing") | `Hub/AiHubRootTests.cs:48,63,124`; `Hub/AiProjectRoomTests.cs:46,63,77,87,101,132,164` | TEST-CONFIRMED [run: PASS] |
| Settings that do not apply 400 (scenario) | `Hub/AiHubRoomSettingsTests.cs:47,58,82,93,106,119`; a private project 400: `Hub/AiHubRoomSettingsTests.cs:69` (not run yet) | TEST-CONFIRMED [run: PASS] |
| Never archived, no template, no folder in the root (scenario) | `Hub/AiHubRootTests.cs:79,94,109`; `Hub/AiProjectRoomTests.cs:196,210` | TEST-CONFIRMED [run: PASS] |
| Knowledge room needs vectorization 403 | `Security:1424,1443`; `AI/Core/Settings` | CODE-ONLY |
| AI project needs the AI gateway or a profile 403 | `Hub/AiHubNoAiProviderTests.cs:47,59` | TEST-CONFIRMED [run: PASS] |
| **Members of the AI rooms** | | |
| Content creator reads, does not write (scenario); non-member 403 | `Hub/AiProjectModelTests.cs:60,77,106`; `Hub/AiProjectRoomTests.cs:116,148,179` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin outside the project reads its model, sources and skills (scenario) | `Hub/AiRoomOutsiderAccessTests.cs:55` | TEST-CONFIRMED (not run yet) |
| Role sets of Instructions and Knowledge rooms | `Security` (`GetAccesses`); `products/ASC.Files/Core/Helpers` | CODE-ONLY |
| **Users only in an AI project** | | |
| Guest not in a project (scenario); no invitation link | `Hub/AiProjectGuestInviteTests.cs:45,61`; `Hub/AiProjectInvitationLinkTests.cs:47` | TEST-CONFIRMED [run: PASS] |
| Group invited to a project (scenario) | `Hub/AiProjectRolesTests.cs:65` | TEST-CONFIRMED [run: PASS] |
| **The model of an AI project** | | |
| Null while none selected; Instructions / unknown 404 | `Hub/AiProjectModelTests.cs:46,93,165` | TEST-CONFIRMED [run: PASS] |
| Member refused; unknown, empty 400 (scenario "Selection cannot be cleared") | `Hub/AiProjectModelTests.cs:106,123,137,150` | TEST-CONFIRMED [run: PASS] |
| Image profile refused (scenario) | `T/ChatTests/ChatProjectModelTests.cs:238,255` | TEST-CONFIRMED [run: PASS] |
| Content creator reads the fixed model (scenario) | `Hub/AiProjectModelTests.cs:60` | TEST-CONFIRMED [run: PASS] (`ChatProjectModelTests.GetModel_ProjectMember_…` fails later, at the round on the project's model: A16, not this claim) |
| Model moved (scenario) | `T/ChatTests/ChatProjectModelTests.cs:94` | TEST-CONFIRMED [run: PASS] |
| Audited, pushed | `products/ASC.Files/Server/Api/VirtualRoomsController.cs` (`setProjectModel`) | CODE-ONLY |
| **Sources of an AI project** | | |
| Sorted; duplicates once (scenario) | `Hub/AiProjectSourcesLinkTests.cs:42,56,77` | TEST-CONFIRMED [run: PASS] |
| Refused whole: other type, unknown, deleted, empty, over 40, unreadable, Viewer (scenario "Room not linkable") | `Hub/AiProjectSourcesLinkTests.cs:94,111,125,138,153,166,179,197,217` | TEST-CONFIRMED [run: PASS] |
| Over the limit (scenario); deleted room (scenario); unlink | `Hub/AiProjectSourcesUnlinkTests.cs:42,60,113,129` | TEST-CONFIRMED [run: PASS] |
| Manager only | `Hub/AiProjectSourcesLinkTests.cs:231`; `Hub/AiProjectSourcesUnlinkTests.cs:73,91` | TEST-CONFIRMED [run: PASS] |
| Audited and pushed | `products/ASC.Files/Core` (share and socket calls in the link service) | CODE-ONLY |
| **Access through a project** | | |
| Member reads linked room; non-member 403; not listed in members | `Hub/AiProjectMemberAccessTests.cs:45,67,142` | TEST-CONFIRMED [run: PASS] |
| Unlink / removal ends it (scenario); own role not lowered | `Hub/AiProjectMemberAccessTests.cs:85,104,122` | TEST-CONFIRMED [run: PASS] |
| Skills for a member | `Hub/AiProjectMemberAccessTests.cs:177`; `Hub/AiRoomSkillsTests.cs:62` | TEST-CONFIRMED [run: PASS] |
| **Number of projects that use a room** | | |
| Room linked to two projects (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:46` | TEST-CONFIRMED (not run yet) |
| Room linked to no project (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:58` | TEST-CONFIRMED (not run yet) |
| Room changed (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:69` | TEST-CONFIRMED (not run yet) |
| Rooms of other types (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:80` | TEST-CONFIRMED (not run yet) |
| Reader of the room outside the projects (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:96` | TEST-CONFIRMED (not run yet) |
| Number follows the links (scenario) | `Hub/AiRoomLinkedProjectsCountTests.cs:109` | TEST-CONFIRMED (not run yet) |
| The same for a Knowledge room: the tests use an Instructions room, the stand has no vectorization | `products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs` (`CarriesLinkedProjectsCount`) | CODE-ONLY |
| **Listing the projects that use a room** | | |
| Projects sorted by title (scenario) | `Hub/AiRoomProjectsListTests.cs:45` | TEST-CONFIRMED (not run yet) |
| Projects with one title (scenario) | `Hub/AiRoomProjectsListTests.cs:58` | TEST-CONFIRMED (not run yet) |
| Room linked to no project (scenario) | `Hub/AiRoomProjectsListTests.cs:69` | TEST-CONFIRMED (not run yet) |
| Id that is no Knowledge or Instructions room (scenario) | `Hub/AiRoomProjectsListTests.cs:84` | TEST-CONFIRMED (not run yet) |
| Caller who cannot read the room (scenario) | `Hub/AiRoomProjectsListTests.cs:102` | TEST-CONFIRMED (not run yet) |
| One page (scenario) | `Hub/AiRoomProjectsPagingTests.cs:46` | TEST-CONFIRMED (not run yet) |
| Start index past the end (scenario) | `Hub/AiRoomProjectsPagingTests.cs:58` | TEST-CONFIRMED (not run yet) |
| Count of zero, negative count, count above the maximum, empty count, negative start index (scenarios) | `Hub/AiRoomProjectsPagingTests.cs:73,85` | TEST-CONFIRMED (not run yet) |
| Largest count (scenario) | `Hub/AiRoomProjectsPagingTests.cs:97` | TEST-CONFIRMED (not run yet) |
| **Who is listed among the projects of a room** | | |
| Member of one of two projects (scenario) | `Hub/AiRoomProjectsVisibilityTests.cs:47` | TEST-CONFIRMED (not run yet) |
| Reader of the room outside the projects (scenario) | `Hub/AiRoomProjectsVisibilityTests.cs:66` | TEST-CONFIRMED (not run yet) |
| Member who reads the room through a project (scenario) | `Hub/AiRoomProjectsVisibilityTests.cs:83` | TEST-CONFIRMED (not run yet) |
| DocSpace admin outside the projects (scenario) | `Hub/AiRoomProjectsVisibilityTests.cs:98` | TEST-CONFIRMED (not run yet) |
| Creator of a project (scenario) | `Hub/AiRoomProjectsVisibilityTests.cs:111` | TEST-CONFIRMED (not run yet) |
| **Clients are told when the number of projects of a room changes** | | |
| Room linked (scenario) | `products/ASC.Files/Tests/Tests/05_Features/Socket/SocketAiRoomLinkEventsTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Room unlinked (scenario) | `products/ASC.Files/Tests/Tests/05_Features/Socket/SocketAiRoomLinkEventsTests.cs:62` | TEST-CONFIRMED [run: PASS] |
| Project deleted (scenario) | `products/ASC.Files/Tests/Tests/05_Features/Socket/SocketAiRoomLinkEventsTests.cs:81` | TEST-CONFIRMED [run: PASS] |
| Nothing linked anew (scenario) | `products/ASC.Files/Tests/Tests/05_Features/Socket/SocketAiRoomLinkEventsTests.cs:100` | TEST-CONFIRMED [run: PASS] |
| **Skills of an Instructions room** | | |
| Front matter rules, upload, chunked, new version (scenario "Invalid name") | `Instr/SkillUploadTests.cs:41-221` | TEST-CONFIRMED [run: PASS] |
| Copy and move refused; no folder | `Instr/SkillMoveCopyTests.cs:58,90,107`; `Instr/InstructionsRoomFolderTests.cs:41,56,75,91` | TEST-CONFIRMED [run: PASS] |
| Skill follows its file (scenario) | `Hub/RoomsSkillsLifecycleTests.cs:41,58,73,88,105,124,139,155`; `Instr/SkillMoveCopyTests.cs:123,157` | TEST-CONFIRMED [run: PASS] |
| **Creating a skill from the template** | | |
| Route `POST skills/create`; title trimmed, trailing `.md` dropped, 1–165 characters, a letter or digit (400); the room checked before the write: unknown 404, not an Instructions room 400, no create right 403; template frontmatter `name` from `toSkillName`, `description` the title; saved as `<title>.md` through the Files insert route, which numbers a taken title | `Chat/routes.ts:212`; `Chat/skills/createSkill.ts:53-116`; `Chat/skills/generateSkill.ts:150-165,292-303`; `Chat/skills/__tests__/createSkill.test.ts:56,69` (frontmatter and body of the template; saved into the room as `<title>.md`); yaml `aiSkillsCreate` (6333) | TEST-CONFIRMED [run: NO-LINK] at unit level (template, file name), CODE-ONLY (status codes, numbered title) |
| **Listing the skills** | | |
| `getRoomSkills` | `Hub/AiRoomSkillsTests.cs:46,62,82,95,108,118,134` | TEST-CONFIRMED [run: PASS] |
| `getRoomsSkills` (scenario "Unreadable room left out") | `Hub/RoomsSkillsTests.cs:41,61,76,94,107,120,137,147` | TEST-CONFIRMED [run: PASS] |
| `getRoomAiFolder` (scenario "Room without a skill") | `T/SkillTests/SkillFolderListingTests.cs:48,58,69` | TEST-CONFIRMED [run: PASS] |
| `getRoomAiFolder` non-reader 403; `withAiFolder` (scenario) | `products/ASC.Files/Server/Api/VirtualRoomsController.cs`; yaml 30261; `Hub/AiRoomOutsiderAccessTests.cs:75,91,107,129` | TEST-CONFIRMED [run: FAIL-BUG] for the 403 and the reader's 200 (`AiRoomOutsiderAccessTests.cs:75,91`) and for `withAiFolder` with `searchArea=AiHub` (`:107`); for `withAiFolder` alone as the operation's description advises (`:129`, A34) |
| **Using the skills in a chat** | | |
| Index and `read_skill`; body only on request (scenario "Model reads one skill") | `T/SkillTests/SkillChatTests.cs:46,73,93` | TEST-CONFIRMED [run: PASS] |
| Member yes, outsider, Guest, revoked no (scenario "Outsider") | `T/SkillTests/SkillPermissionTests.cs:80,98,138,153,183,209` | TEST-CONFIRMED [run: PASS] |
| Composer lists the rooms (scenario) | `Chat/controllers/contextController.ts`; yaml `aiContextGetContextFolders`, `aiContextGetRoomSkills` | CODE-ONLY |
| **An AI agent as a room: the deltas** | | |
| Roles, Guest Viewer only (scenario "Guest as Content creator") | `T/AttachmentTests/AttachmentDeviceAccessTests.cs:77,115`; `T/ChatTests/ChatPermissionGuestTests.cs:186` | TEST-CONFIRMED [run: PASS] |
| Root holds no files; Viewer, Content creator (scenario "Upload to the agent root") | `T/AttachmentTests/AttachmentDestinationTests.cs:107,152`; `AttachmentDeviceAccessTests.cs:46,97` | TEST-CONFIRMED [run: PASS] |
| Knowledge: no subfolders | `T/AttachmentTests/AttachmentDestinationTests.cs:127` | TEST-CONFIRMED [run: PASS] |
| Knowledge: indexable formats only | `TS/vectorization/vectorization.validation.spec.ts:144`; `Security` | TS-ONLY (TS block skipped) |
| Imposes no model (scenario); stored prompt (scenario) | `T/ChatTests/ChatAiRoomTests.cs:46`; `T/ChatTests/ChatComposerAgentTests.cs:53,108` | TEST-CONFIRMED [run: PASS] |
| No template (scenario) | `TS/agents/agents.spec.ts` template block | TS-ONLY (TS block skipped) |
| **Starting a chat thread** | | |
| Roles; Guest 403 | `T/ChatTests/ChatStartTests.cs:49`; `ChatPermissionGuestTests.cs:93`; `ChatPermissionAnonymousTests.cs:74` | TEST-CONFIRMED [run: PASS] |
| Blank title (scenario), no body | `T/ThreadStorageTests/ThreadsCreateTests.cs:102,114,131`; `T/ChatTests/ChatFirstMessageTests.cs:78,93` | TEST-CONFIRMED [run: PASS] |
| Limit (scenario); parallel (scenario); verbatim | `T/ThreadStorageTests/ThreadTitleLengthTests.cs:43,57`; `ThreadsCreateTests.cs:89,199` | TEST-CONFIRMED [run: PASS] |
| `profileId` malformed 400, unknown 404; no profile (scenario) | `T/ChatTests/ChatThreadModelRulesTests.cs:84,144`; `T/ThreadStorageTests/ThreadCreateTests.cs:98` | TEST-CONFIRMED [run: PASS] |
| Listed empty until a message; creator alone | `T/ChatTests/ChatFirstMessageTests.cs:49`; `ChatGlobalEntryTests.cs:89` | TEST-CONFIRMED [run: PASS] |
| **Where a thread may be started** | | |
| Omitted / null ordinary; malformed and unknown 404; any accessible folder | `T/ThreadStorageTests/ThreadsCreateTests.cs:143,170,184`; `T/ChatTests/ChatPermissionValidationTests.cs:64` | TEST-CONFIRMED [run: PASS] |
| Empty or 0 as ordinary | `Chat/storage/docspaceFilesApi.ts:713` (empty); 0 by `IntegrationServiceBase` | CODE-ONLY |
| Non-member (scenario); Read member (scenario) | `T/ChatTests/ChatPermissionNonMemberTests.cs:57,64,79,104`; `ChatPermissionRevokedTests.cs:52` | TEST-CONFIRMED [run: PASS] |
| Outsider of a project (scenario) | `T/ChatTests/ChatProjectAccessTests.cs:48` | TEST-CONFIRMED [run: PASS] |
| **Thread privacy** | | |
| Creator only on every route (scenario "Two members") | `T/ThreadStorageTests/ThreadsPermissionTests.cs:67`; `ThreadsLifecycleTests.cs:209`; `T/ChatTests/ChatPermissionIsolationTests.cs:77`; `ChatPermissionIsolationSendTests.cs:51` | TEST-CONFIRMED [run: PASS] |
| Removed member (scenario) | `T/ChatTests/ChatPermissionRevokedTests.cs:52,67,90` | TEST-CONFIRMED [run: PASS] |
| Member removed from an AI project (scenario) | `T/ProjectMemberRemovalTests/ProjectMemberRemovalTests.cs:43` | TEST-CONFIRMED |
| Regeneration by a fellow member (scenario) | `T/MessageStorageTests/MessageRegenerateAccessTests.cs:55,79` | TEST-CONFIRMED [run: PASS] |
| **One thread list per user** | | |
| Rooms share one list (scenario); third-party | `T/ChatTests/ChatEntityContextTests.cs:71,108,139`; `ChatGlobalEntryTests.cs:146`; `T/ThirdPartyTests/ThirdPartyThreadTests.cs:68` (bug 82855, by design) | TEST-CONFIRMED [run: PASS] |
| Unopenable room lists the same | `T/ChatTests/ChatRoomAcrossUsersTests.cs:47`; `T/ThirdPartyTests/ThirdPartyThreadTests.cs:88` | TEST-CONFIRMED [run: PASS] |
| Room deleted (scenario) | `T/ChatTests/ChatRoomGoneTests.cs:45,86` | TEST-CONFIRMED [run: PASS] |
| **Listing, paging and searching threads** | | |
| Newest first, `count`, 0 → 400, deleted leaves | `T/ChatTests/ChatThreadListingTests.cs:46,73`; `T/ThreadStorageTests/ThreadsListTests.cs:46,73,92` | TEST-CONFIRMED [run: PASS] |
| Cursor (scenario); search (scenario) | `T/ThreadStorageTests/ThreadsListTests.cs:102,137`; `ThreadReadTests.cs:151`; `ThreadSearchTests.cs:53,74` | TEST-CONFIRMED [run: PASS] |
| Touch moves only that thread, never back | `T/ChatTests/ChatThreadManagementTests.cs:46`; `T/ThreadStorageTests/ThreadsTouchTests.cs:72` | TEST-CONFIRMED [run: PASS] |
| **Renaming, deleting and clearing a thread** | | |
| Rename verbatim, refused body, unknown | `T/ThreadStorageTests/ThreadsRenameTests.cs:57,97,119,137`; `T/ChatTests/ChatThreadValidationTests.cs:54` | TEST-CONFIRMED [run: PASS] |
| Delete; gone everywhere (scenario) | `T/ThreadStorageTests/ThreadsDeleteClearTests.cs:48,75,88`; `T/ChatTests/ChatThreadValidationTests.cs:90,111` | TEST-CONFIRMED [run: PASS] |
| Clear; cleared carries on (scenario) | `T/ThreadStorageTests/ThreadsDeleteClearTests.cs:100,144`; `T/ChatTests/ChatClearedThreadTests.cs:50`; `ChatThreadValidationTests.cs:140` | TEST-CONFIRMED [run: PASS] |
| Rename mid-reply (scenario) | `T/ChatTests/ChatMutatedWhileWritingTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| Malformed id 400 | `T/ChatTests/ChatThreadValidationTests.cs:76` | TEST-CONFIRMED [run: PASS] |
| **Touching and reopening a thread** | | |
| Touch | `T/ThreadStorageTests/ThreadsTouchTests.cs:49,91,107` | TEST-CONFIRMED [run: PASS] |
| Replay; another profile (scenario); no profile object 404 | `T/ChatTests/ChatOpenOrCreateTests.cs:67,104,137,168`; `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:47,108` | TEST-CONFIRMED [run: PASS] |
| Opened by the first message (scenario) | `T/ChatTests/ChatOpenOrCreateTests.cs:198`; `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:132` | TEST-CONFIRMED [run: PASS] |
| **Regenerating a thread title** | | |
| Refused request (scenario) | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:52,78`; `T/ChatTests/ChatRegenerateTitleTests.cs:73` | TEST-CONFIRMED [run: PASS] |
| Title from a conversation (scenario), repeat | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:102`; `T/ChatTests/ChatRegenerateTitleTests.cs:51` | TEST-CONFIRMED [run: PASS] |
| Foreign 403, AI off | `T/ThreadStorageTests/ThreadsPermissionTests.cs:67`; `ThreadsAiDisabledTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| **Messages of a thread** | | |
| Append verbatim, no answer; parallel (scenario) | `T/MessageStorageTests/ThreadMessagesAppendTests.cs:43,70,82,122` | TEST-CONFIRMED [run: PASS] |
| Refusals; assistant by hand (scenario) | `T/MessageStorageTests/ThreadMessagesAppendValidationTests.cs:51,75,103,124,145,165` | TEST-CONFIRMED [run: PASS] |
| Too many attachments: 200 and stored with all 11 (scenario) | failure text of `AttachmentComposerRuleTests.AppendUserMessage_MoreAttachmentsThanTheClientAllows_IsNotStoredWhole` and `SendWithStream_MoreAttachmentsThanTheClientAllows_IsNotStoredWhole` in run 365d87fe60 (bug 82894, A28); run 2026-11-10: `AttachmentComposerRuleTests.cs:108,129` red again | TEST-CONFIRMED [run: FAIL-BUG] - `T/AttachmentTests/AttachmentComposerRuleTests.cs:108,129` (A28) |
| Reading one message (scenario) | `T/MessageStorageTests/ThreadMessagesGetByIdTests.cs:46,70,101`; `MessageReadOneTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Update in place (scenario "Edited question") | `T/MessageStorageTests/ThreadMessagesUpdateTests.cs:43`; `ThreadMessagesUpdateValidationTests.cs:52,103,131`; `MessageRewriteQuestionTests.cs:49,95` | TEST-CONFIRMED [run: PASS] |
| Deleting a message (scenario) | `T/MessageStorageTests/ThreadMessagesDeleteTests.cs:99,118` | TEST-CONFIRMED [run: PASS] |
| Another user's message 403 | `T/MessageStorageTests/MessageCrossUserTests.cs:46,66` | TEST-CONFIRMED [run: PASS] |
| **Reading the history** | | |
| Oldest first, count, 400, 404, missing id | `T/MessageStorageTests/ThreadMessagesReadTests.cs:47,100,118,132,158` | TEST-CONFIRMED [run: PASS] |
| Paging back (scenario); cursor (scenario); bare id | `T/MessageStorageTests/MessageReadTests.cs:153,192`; `MessagePagingTests.cs:51` | TEST-CONFIRMED [run: PASS] |
| Raw markdown, growing snapshots | `T/MessageStorageTests/MessageCopyTests.cs:47,78`; `MessageMarkdownReplyTests.cs:47,143` | TEST-CONFIRMED [run: PASS] |
| **Sending a message with a streamed reply** | | |
| Stored, NDJSON; first send (scenario) | `T/ChatTests/ChatSendTests.cs:52`; `ChatReplyStreamTests.cs:55`; `ChatFirstMessageTests.cs:114,164` | TEST-CONFIRMED [run: PASS] |
| Context stays inside (scenario) | `T/ChatTests/ChatSendTests.cs:72,102` | TEST-CONFIRMED [run: PASS] |
| Hang-up (scenario); provider refuses (scenario) | `T/MessageStorageTests/MessageStopTests.cs:58,91`; `T/ChatTests/ChatProviderFailureTests.cs:51` | TEST-CONFIRMED [run: PASS] |
| **Refusals of a send** | | |
| Empty, unknown thread, foreign, Guest, AI off | `T/ChatTests/ChatPermissionValidationTests.cs:81,98`; `ChatPermissionIsolationSendTests.cs:51`; `ChatPermissionGuestTests.cs:118`; `ChatAiDisabledTests.cs:115` | TEST-CONFIRMED [run: PASS] |
| Unknown profile (scenario), malformed | `T/ChatTests/ChatThreadModelRulesTests.cs:168`; `ChatHiddenFailureTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Non-member of project; oversized (scenario) | `T/ChatTests/ChatProjectAccessTests.cs:84`; `ChatHiddenFailureTests.cs:77` | TEST-CONFIRMED [run: PASS] |
| Unpaid 402 | `TS/chat/chat.ai-disabled.spec.ts:352` | TS-ONLY (TS block skipped) |
| Project without a usable model 409 (scenario) | `Chat/storage/aiProjectChatContext.ts:259-275`; `Chat/storage/__tests__/aiProjectChatContext.test.ts`; yaml 743 ff. | TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] at unit level - `Chat/storage/__tests__/aiProjectChatContext.test.ts:276,295,342`; no .NET test drives it |
| **The model of a chat** | | |
| Project answers on its model, for a manager (scenario); blank, unknown | `T/ChatTests/ChatProjectModelTests.cs:47,74,186,212`; `Send_OtherProfileInProject_CannotOverrideTheFixedModel(asMember: False)` | TEST-CONFIRMED [run: PASS] |
| The same send by a read member misses the project's model | failure text of `Send_OtherProfileInProject_CannotOverrideTheFixedModel(asMember: True)` in run 365d87fe60 (bug 82914, A16); run 2026-11-10: red again | TEST-CONFIRMED [run: FAIL-BUG] - `T/ChatTests/ChatProjectModelTests.cs:278` (A16) |
| Thread's own, Chat, Default; rebinding moves nothing | `T/ChatTests/ChatThreadModelTests.cs:50,99,198`; `ChatThreadModelRulesTests.cs:49,113,183`; `ChatOtherActorTests.cs:45`; `ChatRoomModelTests.cs:87` | TEST-CONFIRMED [run: PASS-ON-RERUN] |
| Picker switches (scenario) | `T/ChatTests/ChatThreadModelTests.cs:129,165` | TEST-CONFIRMED [run: PASS] |
| Image-only (scenario) | `T/ChatTests/ChatImageTests.cs:84` | TEST-CONFIRMED [run: PASS] |
| **One-shot sends** | | |
| No thread, no history (scenario) | `T/MessageStorageTests/MessageOneShotSendTests.cs:49,189`; `MessageOneShotSendCustomTests.cs:137` | TEST-CONFIRMED [run: PASS] |
| Empty, malformed, Default, entity rules, `systemPrompt`, `isStream` | `T/MessageStorageTests/MessageOneShotSendTests.cs:89,116,144,160`; `MessageOneShotSendCustomTests.cs:46,80,95` | TEST-CONFIRMED [run: PASS] |
| Guest 403 | `T/ChatTests/ChatPermissionGuestTests.cs:118` | TEST-CONFIRMED [run: PASS] |
| **OpenAI-shaped send** | | |
| Stream (scenario), persist, model, hang-up | `T/MessageStorageTests/MessageOpenAiStreamTests.cs:45`; `MessageOpenAiStreamThreadTests.cs:47,74,106,165` | TEST-CONFIRMED [run: PASS] |
| Tool call (scenario); failure inside the stream | `T/MessageStorageTests/MessageOpenAiStreamTests.cs:73,149` | TEST-CONFIRMED [run: PASS] |
| **Regenerating a reply** | | |
| Last reply replaced; multi-turn (scenario) | `T/MessageStorageTests/MessageRegenerateTests.cs:48,117,160` | TEST-CONFIRMED [run: PASS] |
| Thread's model, project's model | `T/MessageStorageTests/MessageRegenerateModelTests.cs:50`; `MessageRegenerateProjectTests.cs:47` | TEST-CONFIRMED [run: PASS] |
| Tool chain whole; earlier version kept (scenario); abandoned (scenario) | `T/McpTests/McpToolCallRegenerateTests.cs:101,149,189`; `T/MessageStorageTests/MessageStopTests.cs:130` | TEST-CONFIRMED [run: PASS-ON-RERUN] (the mark is of run 80268dcdbe; the three regenerate tests were rewritten after it, in 5c9fa827bb) |
| Nothing to regenerate, unknown, foreign, AI off | `T/MessageStorageTests/MessageRegenerateTests.cs:87,104`; `MessageRegenerateAccessTests.cs:55,107` | TEST-CONFIRMED [run: PASS] |
| **Tool calls and the approval pause** | | |
| Pause, approve string / structured, deny (scenario "Deny") | `T/McpTests/McpToolCallPauseTests.cs:54,89,123,140` | TEST-CONFIRMED [run: FAIL-BUG] |
| Two-hop (scenario) | `T/McpTests/McpToolCallPauseTests.cs:165` | TEST-CONFIRMED [run: PASS] |
| Announcement (scenario) | `T/McpTests/McpToolCallAnnouncementTests.cs:81,112,138,156` | TEST-CONFIRMED [run: PASS] |
| Opt-out and pre-approved | `T/McpTests/McpAllowAlwaysPauseTests.cs:66,80,92` | TEST-CONFIRMED [run: PASS] |
| Unavailable, withdrawn, server left; switched off (scenario) | `T/McpTests/McpHostToolsChatTests.cs:115,184,210`; `McpRegisteredServerChatTests.cs:137,160` | TEST-CONFIRMED [run: PASS-ON-RERUN] |
| **Decisions on a tool call** | | |
| Nothing to decide (scenario), unknown, out of range 409 | `T/McpTests/McpToolCallDecisionTests.cs:70,91` | TEST-CONFIRMED [run: PASS] |
| Anonymous, non-member, AI off | `T/McpTests/McpToolCallDecisionTests.cs:103,120,143` | TEST-CONFIRMED [run: PASS] |
| Replay (scenarios "Replayed approval", "Replayed denial"), stale | `T/McpTests/McpToolCallReplayTests.cs:63,76,89,106,123` | TEST-CONFIRMED [run: PASS] |
| `message` snapshot ignored | `common/ASC.AI.Chat/scripts/schema/schemaTypes.ts:304-339`; yaml 71724 (`message` optional) | CODE-ONLY |
| **Deep mode and reasoning level** | | |
| Off and unset; on, off, clear; on after off (scenario) | `T/PreferenceTests/PreferenceDeepModeStateTests.cs:46-120` | TEST-CONFIRMED [run: PASS] |
| Levels | `T/PreferenceTests/PreferenceReasoningLevelTests.cs:47,55,70` | TEST-CONFIRMED [run: PASS] |
| One user's value (scenario) | `T/PreferenceTests/PreferencePermissionTests.cs:92,133` | TEST-CONFIRMED [run: PASS] |
| Reasoning reaches the client (scenario) | `T/PreferenceTests/PreferenceDeepModeChatTests.cs:87,119` | TEST-CONFIRMED [run: PASS] |
| **Scope and refusals of the preferences** | | |
| Room is no scope (scenario); unopenable, unknown | `T/PreferenceTests/PreferenceScopeTests.cs:52,66,83,100,116,126`; `PreferencePermissionTests.cs:111` | TEST-CONFIRMED [run: PASS] |
| Thread as scope (scenario); malformed; empty and 0 | `T/PreferenceTests/PreferenceEntityIdTests.cs:73,90,106,126,146` | TEST-CONFIRMED [run: PASS] |
| Non-boolean, empty, bad depth | `T/PreferenceTests/PreferenceDeepModeValidationTests.cs:49,62,84`; `PreferenceReasoningLevelTests.cs:89`; `PreferenceClearTests.cs:52` | TEST-CONFIRMED [run: PASS] |
| Guest, anonymous, AI off | `T/PreferenceTests/PreferencePermissionTests.cs:47,78`; `PreferenceAiDisabledTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| **Custom MCP servers** | | |
| Roles (scenario "Member cannot register") | `T/McpTests/McpToolsManagePermissionsTests.cs:46`; `McpToolsMemberPermissionsTests.cs:46`; `McpToolsAccessTests.cs:138`; `McpToolsAnonymousTests.cs:59` | TEST-CONFIRMED [run: NOT-IN-RUN] (a cited test was renamed and its expectation changed after run 80268dcdbe) |
| Round trip (scenario), no dial, verbatim, flag (scenario) | `T/McpTests/McpServerRegistryTests.cs:50,93,135,349,366` | TEST-CONFIRMED [run: PASS] |
| Unregistered name (scenario) | `T/McpTests/McpServerRegistryTests.cs:260` | TEST-CONFIRMED [run: PASS] |
| AI off (scenario), off period (scenario) | `T/McpTests/McpToolsAiDisabledTests.cs:58,82`; `McpToolsAccessTests.cs:287` | TEST-CONFIRMED [run: NOT-IN-RUN] (a cited test was renamed and its expectation changed after run 80268dcdbe) |
| Audited | `AI/Server/Service/McpServerStorageService.cs` (`ServerCreated/Updated/Deleted`) | CODE-ONLY |
| **One registry per portal** | | |
| Registered from a room (scenario); folder | `T/McpTests/McpServerEntityScopeTests.cs:49,66,77,97`; `T/ThirdPartyTests/ThirdPartyToolsTests.cs:68,80` (skips on the stand) | TEST-CONFIRMED [run: PASS] |
| Unknown room (scenario), deleted, unopenable | `T/McpTests/McpServerEntityScopeTests.cs:160,190`; `McpToolsAccessTests.cs:209` | TEST-CONFIRMED [run: PASS] |
| Reads ignore the entity (scenario) | `T/McpTests/McpServerEntityScopeTests.cs:146` | TEST-CONFIRMED [run: PASS] |
| Name without config | `T/McpTests/McpServerRegistryTests.cs:236,247` | TEST-CONFIRMED [run: PASS] |
| **Server names** | | |
| Limit (scenario); hard refusals; blank | `T/McpTests/McpServerNameTests.cs:59,83,94,242`; `McpServerReplaceAllTests.cs:200` | TEST-CONFIRMED [run: PASS] |
| Underscore (scenario); replace-all per entry | `T/McpTests/McpServerNameTests.cs:137,148` | TEST-CONFIRMED [run: PASS] |
| Unicode (scenario) | `T/McpTests/McpServerNameTests.cs:206,223` | TEST-CONFIRMED [run: PASS] |
| Casing, whitespace; soft refusals (scenario); removal (scenario) | `T/McpTests/McpServerRegistryTests.cs:170,184,195,209,222,305,332` | TEST-CONFIRMED [run: PASS] |
| **Server configurations and replace-all** | | |
| Both url and command (scenario); wrong type; empty; missing | `T/McpTests/McpServerConfigValidationTests.cs:56,67,79,110,124,142,164` | TEST-CONFIRMED [run: PASS] |
| Unusable map (scenario) | `T/McpTests/McpServerReplaceAllTests.cs:93,107,121,134` | TEST-CONFIRMED [run: PASS] |
| Exact map; empty; one bad entry (scenario); case twins (scenario) | `T/McpTests/McpServerReplaceAllTests.cs:50,81,145,162,227` | TEST-CONFIRMED [run: PASS] |
| **Switching tools off** | | |
| Disable, re-enable (scenario), replacement, read routes | `T/McpTests/McpDisabledToolsTests.cs:49,64,109,133,248,258,267` | TEST-CONFIRMED [run: PASS] |
| Server type and list refusals (scenario "Refused list") | `T/McpTests/McpDisabledToolsTests.cs:164,186,201,217,233` | TEST-CONFIRMED [run: PASS] |
| Per user | `T/McpTests/McpToolPrefsLifecycleTests.cs:146`; `McpToolsSettingsPermissionsTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Dropped server (scenario) | `T/McpTests/McpToolPrefsLifecycleTests.cs:45,81`; `T/McpServerStorageTests/McpServerReplaceTests.cs:139` | TEST-CONFIRMED [run: PASS] |
| Guest, AI off naming a server (scenario) | `T/McpTests/McpToolsAccessTests.cs:320,340` | TEST-CONFIRMED [run: PASS] |
| Portal-wide list for everyone (`scope` `tenant`) | `Chat/controllers/toolsController.ts` (`parseToolPrefsScope`, bug 84381); `T/McpTests/McpToolPrefsTenantScopeTests.cs:63,81,96` | TEST-CONFIRMED [run: PASS] - read by every member and folded into each member's own list, written by an administrator only (403 otherwise), emptied by an empty list (`McpToolPrefsTenantScopeTests.cs:63,81,96`) |
| **Pre-approving tools** | | |
| Flat tokens, idempotent, revoke, falsy, string, missing | `T/McpTests/McpAllowAlwaysTests.cs:46,57,67,127,144,158,173,186` | TEST-CONFIRMED [run: PASS] |
| One user's approval (scenario); independent of disabled | `T/McpTests/McpToolPrefsLifecycleTests.cs:101,128` | TEST-CONFIRMED [run: PASS] |
| Approval with `allowAlways` (scenario) | `T/McpTests/McpAllowAlwaysPauseTests.cs:92,122,136`; `McpRegisteredServerChatTests.cs:116` | TEST-CONFIRMED [run: PASS] |
| Same name on another server (scenario) | `T/McpTests/McpAllowAlwaysTests.cs:96,110` | TEST-CONFIRMED [run: PASS] |
| **Tools catalogue** | | |
| Groups, no twin, every non-guest role, entity; Guest or AI off 403 (scenario) | `T/McpTests/McpToolsCatalogueTests.cs:48,60,92,108,125,138`; `McpToolsAiDisabledTests.cs:82`; `McpToolsAccessTests.cs:138,287` | TEST-CONFIRMED [run: NOT-IN-RUN] (a cited test was renamed and its expectation changed after run 80268dcdbe) |
| Unreachable server (scenario) | `T/McpTests/McpToolsCatalogueTests.cs:75`; `McpRegisteredServerChatTests.cs:95` | TEST-CONFIRMED [run: PASS] |
| Registered tools reach the model | `T/McpTests/McpRegisteredServerChatTests.cs:85` | TEST-CONFIRMED [run: PASS] |
| **Web-search configuration on a self-hosted portal** | | |
| Probe; wrong key (scenario); unreachable; malformed; business 400 | `T/WebSearchTests/WebSearchConfigureTests.cs:46,62,78,95,112` | TEST-CONFIRMED [run: PASS] |
| `aiWebSearchSetActiveConfig`; refused writes (scenario) | `T/WebSearchTests/WebSearchSetActiveConfigTests.cs:62,75,94,108,118,131`; `WebSearchConfigureTests.cs:121,135` | TEST-CONFIRMED [run: PASS] |
| Guest with a malformed body (scenario); AI off first | `T/WebSearchTests/WebSearchSetActiveConfigTests.cs:148,168`; `WebSearchConfigureTests.cs:154,170` | TEST-CONFIRMED [run: PASS] |
| Audited | `AI/Server/Service/WebSearchStorageService.cs` | CODE-ONLY |
| **Clearing and the hosted add-on** | | |
| Room admin cannot clear (scenario); admin clears; unconfigured; entity body (scenario) | `T/WebSearchTests/WebSearchPermissionsTests.cs:100,112,125`; `WebSearchStateTests.cs:90,102,123` | TEST-CONFIRMED [run: PASS] |
| Clearing audited (scenario) | `AI/Server/Service/WebSearchStorageService.cs` | CODE-ONLY |
| Add-on, every scope, tool (hosted) | `TS/web-search/web-search.spec.ts:128,624`; `T/WebSearchTests/WebSearchChatToolTests.cs:52` (tool only while configured) | TS-ONLY |
| Owner cannot clear the add-on config (scenario); writes 403 | `TS/web-search/web-search.spec.ts:345`; `TS/web-search/web-search.config.spec.ts:57` | TS-ONLY |
| **Reading and probing the web-search configuration** | | |
| Roles (scenario "Guest cannot read"), anonymous, entity rules | `T/WebSearchTests/WebSearchPermissionsTests.cs:68,79`; `T/SettingsTests/SettingsWebSearchPermissionsTests.cs:52,79,104`; `WebSearchStateTests.cs:144,155,171` | TEST-CONFIRMED [run: PASS] |
| Off period (scenario) | `T/WebSearchTests/WebSearchPermissionsTests.cs:142` | TEST-CONFIRMED [run: PASS] |
| Probe; Guest and off dial nothing | `T/WebSearchTests/WebSearchTestConnectionTests.cs:70,89,102,152`; `WebSearchPermissionsTests.cs:167,181,200` | TEST-CONFIRMED [run: PASS] |
| Private base URL (scenario), everywhere (scenario) | `T/WebSearchTests/WebSearchEgressGuardTests.cs:80,92,109`; `WebSearchBaseUrlFormatTests.cs:69` | TEST-CONFIRMED [run: PASS] |
| **Web-search passthrough** | | |
| Relay, query and key (scenario); roles; no provider (scenario) | `T/WebSearchTests/WebSearchPassthroughTests.cs:66,75,92,104,129,191`; `WebSearchPassthroughAccessTests.cs:54,80` | TEST-CONFIRMED [run: PASS] |
| Wrong shape 400; 4xx relayed; 5xx and unreachable 502 | `T/WebSearchTests/WebSearchPassthroughValidationTests.cs:84,101,117`; `WebSearchPassthroughTests.cs:149,164,178` | TEST-CONFIRMED [run: PASS] |
| Attributed to a document (scenario) | `T/WebSearchTests/WebSearchPassthroughAccessTests.cs:99` | TEST-CONFIRMED [run: PASS] |
| **OpenAI-compatible passthrough** | | |
| Relay; every optional field (scenario); stream; image on a text model | `TS/openai-passthrough/openai-passthrough.spec.ts:58,109,143` | TS-ONLY |
| Guest (scenario), anonymous, AI off | `TS/openai-passthrough/openai-passthrough.permission.spec.ts:37,58`; `openai-passthrough.ai-disabled.spec.ts:15,46` | TS-ONLY |
| Gateway proxy path list | `AI/Server/Api/AiGatewayProxyController.cs` | CODE-ONLY |
| **Editor tools catalogue** | | |
| Names, schema, approval; repeated reads | `T/EditorToolTests/EditorToolListTests.cs:51,64,102,111,149,165,186` | TEST-CONFIRMED [run: PASS] |
| Guest refused (scenario); non-guest same; anonymous; AI off | `T/EditorToolTests/EditorToolPermissionTests.cs:48,63,163`; `EditorToolAiDisabledTests.cs:53` | TEST-CONFIRMED [run: NOT-IN-RUN] (a cited test was renamed and its expectation changed after run 80268dcdbe) (the non-guest case skips on the stand) |
| **Calling an editor tool** | | |
| Name rules, argument rules | `T/EditorToolTests/EditorToolCallTests.cs:79,89,104,122`; `EditorToolArgumentTests.cs:45,71,102` | TEST-CONFIRMED [run: PASS] |
| Guest; AI off (scenario) | `T/EditorToolTests/EditorToolPermissionTests.cs:101,113,137`; `EditorToolAiDisabledTests.cs:66,88,104,116` | TEST-CONFIRMED [run: PASS] |
| Tool never exceeds the member (scenario); failure inside `result` | `T/EditorToolTests/EditorToolRestToolTests.cs:92,167` | TEST-CONFIRMED [run: SKIP] (skips on the stand) |
| Generator in a room (scenario); no write (scenario); entity names nothing (scenario) | `T/EditorToolTests/EditorToolEntityIdTests.cs:78,93,110,127,156,182` | TEST-CONFIRMED [run: SKIP] (skips on the stand) |
| **Creating chat attachments** | | |
| Attach by id (scenario), large, picture | `T/AttachmentTests/AttachmentStoredFileTests.cs:80,177,218`; `AttachmentSaveFileTests.cs:46,178` | TEST-CONFIRMED [run: PASS] |
| `path` refusals; no text; archive (scenario) | `T/AttachmentTests/AttachmentSaveFileValidationTests.cs:100,112,184`; `AttachmentSaveFileTests.cs:211`; `AttachmentStoredFileTests.cs:106,136,153,252`; `AttachmentComposerRuleTests.cs:58,72` | TEST-CONFIRMED [run: PASS] |
| Guest (scenario), anonymous, AI off | `T/AttachmentTests/AttachmentPermissionTests.cs:63,103`; `AttachmentAiDisabledTests.cs:150,162,178` | TEST-CONFIRMED [run: PASS] |
| Batch (scenarios "Batch with one bad element", "Batch") | `T/AttachmentTests/AttachmentSaveFilesManyTests.cs:45,97,141,214,230`; `AttachmentCrossUserTests.cs:236` | TEST-CONFIRMED [run: PASS] |
| Fields stored as sent (scenario) | `T/AttachmentTests/AttachmentSaveFileTypeTests.cs:47,75`; `AttachmentSaveFileValidationTests.cs:155`; `AttachmentSaveFileTests.cs:115` | TEST-CONFIRMED [run: PASS] |
| **Reading and deleting attachments** | | |
| Another user's draft (scenario); mixed delete (scenario) | `T/AttachmentTests/AttachmentCrossUserTests.cs:57,77,186,209` | TEST-CONFIRMED |
| Unknown, malformed, empty, missing body | `T/AttachmentTests/AttachmentGetManyTests.cs:45,74,86,119`; `AttachmentGetTests.cs:72,85,106`; `AttachmentDeleteTests.cs:72,104,132` | TEST-CONFIRMED |
| Immediately readable; deleted draft gone after the background pass, source kept (scenario "Deleted draft") | `T/AttachmentTests/AttachmentGetManyTests.cs:130,151`; `AttachmentDeleteTests.cs:86,173` | TEST-CONFIRMED |
| Linked attachment (scenario) | `T/AttachmentTests/AttachmentDeleteTests.cs:209` | TEST-CONFIRMED |
| Draft does not reach the model (scenario) | failure text of `AttachmentSendTests.SendWithStream_DraftPassedAsAttachmentsIdOnly_ReachesTheModel` and `SendWithStream_FullyInlinedAttachmentRecord_ReachesTheModelToo` in run 365d87fe60 (bug 82773, A30); `T/ReasoningTests/ReasoningAttachTests.cs:48` passes and does not decide it; run 2026-11-10: both red again | TEST-CONFIRMED [run: FAIL-BUG] - `T/AttachmentTests/AttachmentSendTests.cs:68,91` (A30) |
| Unpaid portal (scenario) | `TS/attachments/attachments.ai-disabled.spec.ts` | TS-ONLY |
| **Linking attachments to a message** | | |
| Dies with the message (scenario), thread | `T/AttachmentTests/AttachmentLinkToMessageTests.cs:68,89,199,218` | TEST-CONFIRMED [run: PASS] |
| 400 / 404 / 403 refusals; foreign draft | `T/AttachmentTests/AttachmentLinkToMessageTests.cs:111,122,133,145,156,166,177,190`; `AttachmentCrossUserTests.cs:139,265` | TEST-CONFIRMED [run: PASS] |
| **Which drafts a link binds** | | |
| Deleted draft is not linked; draft of another thread; another message of the same thread (scenarios), on the AI binding the operation calls | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:352,370,395`; `common/ASC.AI.Integration/Database/AttachmentQueries.cs` (`UpdateAttachmentBindingsByIdsAsync`) | TEST-CONFIRMED |
| **Suggested questions for a form attachment** | | |
| 200 status and list; unknown (scenario); foreign; bad id; anonymous; AI off | `T/AttachmentTests/AttachmentSuggestedQuestionsTests.cs:63,87,98,106,119,133`; `AttachmentAiDisabledTests.cs:225` | TEST-CONFIRMED [run: PASS] |
| **Exporting markdown into a document** | | |
| 202 no id, rendered | `T/TextToDocxTests/TextToDocxExportTests.cs:69,91,158`; `T/MessageStorageTests/MessageMarkdownDocxTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Format (scenario "Unknown format") | `T/TextToDocxTests/TextToDocxFormatTests.cs:70,86`; `TextToDocxShapesTests.cs:62` | TEST-CONFIRMED [run: PASS] |
| Required; whitespace (scenario); folder refusals | `T/TextToDocxTests/TextToDocxValidationTests.cs:49,75,91`; `TextToDocxFolderIdTests.cs:50,67,84,98,116` | TEST-CONFIRMED [run: PASS] |
| Size limit (scenario) | `T/TextToDocxTests/TextToDocxSizeLimitTests.cs:52,71,101,125` | TEST-CONFIRMED [run: PASS] |
| Before the switch | `T/MessageStorageTests/MessagesAiDisabledTests.cs:59,85,104` | TEST-CONFIRMED [run: PASS] |
| Into Chat outputs (scenario) | `T/TextToDocxTests/TextToDocxThreadExportTests.cs:115` | TEST-CONFIRMED [run: PASS] |
| Unpaid portal (scenario) | `TS/messages/messages.ai-disabled.spec.ts` | TS-ONLY |
| **Where an export may land** | | |
| Author; right to create; Viewer (scenario) | `T/MessageStorageTests/MessagesPermissionRoomAccessTests.cs:52,79,103,128`; `MessagesPermissionForeignFolderTests.cs:46,61,77`; `MessagesPermissionOwnFolderTests.cs:55,73` | TEST-CONFIRMED [run: PASS] |
| Archive, Trash | `T/TextToDocxTests/TextToDocxTargetsTests.cs:47,62,85` | TEST-CONFIRMED [run: PASS] |
| Same title (scenario); truncation; cleared; simultaneous | `T/TextToDocxTests/TextToDocxNamingTests.cs:47,74`; `TextToDocxThreadExportTests.cs:231`; `TextToDocxConcurrencyTests.cs:100` | TEST-CONFIRMED [run: PASS] |
| **Vectorization of knowledge** | | |
| AI off 403, missing file 404 | `T/SettingsTests/SettingsAiDisabledTests.cs:66` | TEST-CONFIRMED [run: PASS] |
| File in My documents (scenario), list rules, Guest, repeats, no handle | `TS/vectorization/vectorization.validation.spec.ts:58,95`; `products/ASC.Files/Core/Vectorization/VectorizationTaskPublisher.cs:55-90`; `Security:682-692,2712-2725` | TS-ONLY (TS block skipped) |
| Status and socket; Knowledge room rights (scenario) | `Security:1430-1449`; `VectorizationTaskPublisher.cs` | CODE-ONLY |
| Knowledge room without vectorization (scenario) | `Security:1443`; room creation in `products/ASC.Files/Core/Core/FileStorageService.cs` | CODE-ONLY |
| **Searching knowledge in a chat** | | |
| One query, room filter, 20-room cap (scenario) | `products/ASC.AI/Core/Knowledge/KnowledgeSearchEngine.cs:46-75` (`roomIds.Contains`); `Chat/storage/aiProjectChatContext.ts` | CODE-ONLY |
| Tool offered only while enabled; `aiContextGetKnowledgeFolders` | `Chat/controllers/contextController.ts`; yaml 1806 | CODE-ONLY |
| **Vectorization settings** | | |
| Self-hosted roles (scenario "Room admin"), anonymous, reset, body (scenario) | `T/SettingsTests/SettingsVectorizationPermissionsTests.cs:58,80,100,116,135,146` | TEST-CONFIRMED [run: PASS] |
| Hosted 403 (scenario) | `TS/settings/settings.permission.spec.ts:439,512` | TS-ONLY |
| Key verified; audited | `AI/Core/Settings/AiSettingsService.cs` | CODE-ONLY |
| **Prompt library** | | |
| Roles | `T/PromptTests/PromptRoleTests.cs:49,71`; `PromptGuestTests.cs:47`; `PromptAnonymousTests.cs:46`; `PromptAiDisabledTests.cs:50` | TEST-CONFIRMED [run: PASS] |
| Private (scenario "Libraries are private"); foreign prompt (scenario); foreign folder (scenario) | `T/PromptTests/PromptIsolationTests.cs:54,74,136,157,172,231,260` | TEST-CONFIRMED [run: PASS] |
| Folder read by id (scenario); folder delete cascades | `T/PromptTests/PromptFolderLifecycleTests.cs:109,130,177`; `PromptIntegrityTests.cs:215` | TEST-CONFIRMED [run: PASS] |
| Operations | `T/PromptTests/PromptLifecycleTests.cs:47,68,91`; `PromptMoveTests.cs:47`; `PromptExportTests.cs:49`; `PromptReadAndListTests.cs:47,176` | TEST-CONFIRMED [run: PASS] |
| Operation names (scenario "Operations of the library"): list of one folder, root for an empty `folderId`, newest first; folders newest first, empty ones too; delete of an unknown or repeated id answers success, a repeated folder delete 404; get of an unknown id 200 empty | `T/PromptTests/PromptReadAndListTests.cs:123,138,176`; `PromptLifecycleTests.cs:103,118`; `PromptFolderLifecycleTests.cs:80,130`; `Chat/controllers/promptsController.ts:319-328` | TEST-CONFIRMED [run: PASS] |
| **Document agent settings** | | |
| Read: DocSpace admin, Room admin, User 200, Guest 403, anonymous 401, AI off 403; write: DocSpace admin only, a refused write keeps the settings | `AI/Server/Api/DocumentAgentController.cs` (internal route, `[AiFeature]`); `AI/Server/Service/DocumentAgentSettingsService.cs:51-111`; `T/DocumentAgentTests/DocumentAgentSettingsPermissionsTests.cs:45,59,69,82,92,102` | TEST-CONFIRMED [run: PASS] (on the internal route `internal/ai/document-agent/settings`; the public route of the chat service only relays it, `Chat/controllers/documentAgentController.ts:65-90`) |
| Defaults (on, no profile, `default`, 150, 32 MiB, no log, no folder); all fields read back; a deleted profile, a deleted or trashed log folder read as null | `T/DocumentAgentTests/DocumentAgentSettingsReadTests.cs:41,55,81,89,100,115,128` | TEST-CONFIRMED [run: PASS] |
| Refused settings (scenario): limits below 1, unknown `reasoning`, unknown profile, third-party folder id 400; unknown or trashed folder 404; folder of another user 403; settings unchanged after a refusal | `T/DocumentAgentTests/DocumentAgentSettingsValidationTests.cs:45,57,67,75,83,91,102,121,133` | TEST-CONFIRMED [run: PASS] |
| Ceilings 150 and 32 MiB and a missing field: refused with 400 by the chat service before the AI service is asked (the AI service itself accepts `int.MaxValue`, `DocumentAgentSettingsValidationTests.cs:44,56`) | `Chat/storage/documentAgentSettingsStorage.ts:52-71`; `Chat/controllers/documentAgentController.ts:75-89`; `Chat/controllers/__tests__/documentAgentController.test.ts:154` | TEST-CONFIRMED [run: PASS] at unit level |
| Agent switched off (scenario): the chat context carries `documentAgentEnabled`, false for every member after an admin stores `enabled` false | `AI/Server/Service/ChatContextService.cs`; `T/DocumentAgentTests/DocumentAgentChatContextTests.cs:41,47,55,64`; `Chat/storage/documentAgentSettingsStorage.ts:148-154` | TEST-CONFIRMED [run: PASS] |
| Settings cannot be read (scenario): the round runs with the defaults and `enabled` false | `Chat/storage/documentAgentSettingsStorage.ts:123-145`; `Chat/storage/__tests__/documentAgentSettingsStorage.test.ts:144,184` | TEST-CONFIRMED [run: NO-LINK] at unit level |
| Task log (scenario): a `log` artifact is skipped when the tool's answer is built (`a7d94e4b81`) | `Chat/tools/documentAgent.ts:375` | CODE-ONLY |
| A save writes "document agent settings updated" to the audit trail | `AI/Server/Service/DocumentAgentSettingsService.cs:76-111`; `common/ASC.Core.Common/Messaging/MessageAction.cs` | CODE-ONLY |
| **Prompt names, texts and bundles** | | |
| Name limit (scenario), not a string, rename | `T/PromptStorageTests/PromptNameLengthTests.cs:43,58`; `T/PromptFolderStorageTests/PromptFolderNameLengthTests.cs:43,61`; `T/PromptTests/PromptFolderLifecycleTests.cs:231`; `PromptCreateValidationTests.cs:53` | TEST-CONFIRMED [run: PASS] |
| Unique, case-insensitive; same name elsewhere (scenario); recheck (scenario) | `T/PromptTests/PromptCreateValidationTests.cs:127,149`; `PromptLifecycleTests.cs:154`; `PromptMoveTests.cs:129,159,185` | TEST-CONFIRMED [run: PASS] |
| Text | `T/PromptTests/PromptLifecycleTests.cs:141,204`; `PromptTextSizeTests.cs:114,128` | TEST-CONFIRMED [run: PASS] |
| Import modes (scenario); refused bundle (scenario) | `T/PromptTests/PromptImportTests.cs:81,108,129`; `PromptImportRoundTripTests.cs:98,119`; `PromptImportValidationTests.cs:94,120,136,154,169,181`; `PromptImportRefusalTests.cs:67,85,104` | TEST-CONFIRMED [run: PASS] |
| **Metadata fields proposed for a file** | | |
| Route rules, trim, dedupe, 50, nothing written (scenario `kind` `folder`) | `Chat/controllers/metaController.ts`; `Chat/meta/generateMeta.ts`; `Chat/meta/__tests__/generateMeta.test.ts`; yaml 5631 | TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] at unit level - `generateMeta.test.ts:72,93,133`, `metaController.test.ts:180`; no .NET test |
| **Metadata values filled from a file** | | |
| `dryRun`, `overwrite`, per-field report (scenario) | `Chat/meta/fillMeta.ts`; `Chat/meta/__tests__/fillMeta.test.ts`; yaml 5871 | TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] at unit level - `fillMeta.test.ts:148,290,336,385,438` |
| **The model and the failures of the metadata routes** | | |
| GenerateMeta else Default, 503; 402, 429, 502, 504 (scenario) | `Chat/meta/common.ts`; `Chat/meta/__tests__/common.test.ts`; `Chat/controllers/__tests__/metaController.test.ts` | TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] at unit level - `common.test.ts:84,99,239,265`, `generateMeta.test.ts:175,189`, `metaController.test.ts:134` |
| **Billing of AI usage on a hosted portal** | | |
| Charges; source (scenario); off stops | `TS/billing/billing.spec.ts:145,255,350,437` | TS-ONLY (TS block skipped) |
| **Spending limits on the AI services** | | |
| Scope left out (scenario) | `Web/04_Portal/Payments/PaymentServiceLimitValidationTests.cs:54,73` | TEST-CONFIRMED [run: PASS] |
| Service layer: owner, cache, read, errors | `CoreTests/TariffServiceServiceLimit{Cache,Owner,Read}Tests.cs`; `CoreTests/AccountingClientServiceLimit{Error,Request}Tests.cs` | TEST-CONFIRMED [run: NO-LINK] |
| Portal limit reached (scenario), personal wins | `TS/limits/limits.enforcement.spec.ts:191,287`; `TS/limits/limits.per-user.spec.ts` | TS-ONLY |
| Roles, 403, 404, 409, audit (scenario) | `Payments` (nine `*ServiceLimit*` actions); `TS/limits/limits.crud.spec.ts`; yaml 52223 | CODE-ONLY |
| **Audit trail and real-time events of AI changes** | | |
| AI access, profile, assign, server, vectorization, web search, user settings, project, quota | `SettingsController.cs`; `ProfileStorageService.cs`; `AssignmentsStorageService.cs`; `McpServerStorageService.cs`; `AiSettingsService.cs`; `WebSearchStorageService.cs` | CODE-ONLY |
| Switch audited (scenario) | `SettingsController.cs` (`MessageAction`); `Web/05_Security/AuditTrail/` tests the audit API only | CODE-ONLY |
| Reads not audited (scenario); socket events | absence of a message in read paths; `AI/Server/Service/AiSocketManager.cs:46-52` | CODE-ONLY |
| **Files of a project thread** |  |  |
| A set per thread (scenario "Two threads of one project"); one folder for saves made at the same moment (scenario) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:105,88`; `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:298` | TEST-CONFIRMED |
| The thread does not name its folders (scenario) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:186` | TEST-CONFIRMED |
| A thread of another entry has neither set | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:123`; `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:315` | TEST-CONFIRMED |
| **Saving an artifact of a thread** |  |  |
| Saved and listed (scenario); same title twice (scenario) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:41,56,72` | TEST-CONFIRMED |
| Thread outside a project 400, unknown thread 404, thread of another member 403 (scenarios) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:123,133,141` | TEST-CONFIRMED |
| Empty file 400, file over 100 MB 400 (scenarios) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:158,172` | TEST-CONFIRMED |
| **Listing the files of a thread** |  |  |
| Thread without files; one page with the total (scenarios) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:200,212` | TEST-CONFIRMED |
| Project owner asks for a member's thread 404 (scenario); thread of another project 404; caller outside the project 403 (scenarios) | `T/ThreadArtifactsTests/ThreadArtifactsTests.cs:226,247,260`; `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:429` | TEST-CONFIRMED |
| Deleted thread 404 | `T/ThreadDeletionTests/ThreadDeletionTests.cs:61` | TEST-CONFIRMED |
| Attachment that was not sent (scenario) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:415` | TEST-CONFIRMED |
| `count` 0 and 101 and `startIndex` -1 answer 400; `count` 100 answers 200 (scenarios) | `T/ThreadArtifactsTests/ThreadFilesListBoundsTests.cs:47,60` | TEST-CONFIRMED |
| `count` is 100 when omitted | `products/ASC.Files/Server/ApiModels/RequestDto/GetAiThreadFilesRequestDto.cs:61` | CODE-ONLY |
| **Uploading an attachment into a project** |  |  |
| Member uploads (scenario) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:44,54` | TEST-CONFIRMED |
| Caller outside the project 403, relative path 403, unsupported format 400, upload in one request 403 (scenarios) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:67,80,90,100` | TEST-CONFIRMED |
| **Copying a portal file into a project** |  |  |
| Member copies; same file twice (scenarios) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:112,124,183` | TEST-CONFIRMED |
| Caller outside the project 403, folder 403, unsupported format 415, move 403 (scenarios) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:138,154,170,196` | TEST-CONFIRMED |
| **Attachment of a file stored in a project** |  |  |
| Document attached twice; picture attached twice (scenarios) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:209,225` | TEST-CONFIRMED |
| Deleted attachment attached again (scenario) | `T/UnsentAttachmentCleanupTests/UnsentAttachmentCleanupTests.cs:69` | TEST-CONFIRMED |
| **A sent attachment is kept with its thread** |  |  |
| Linked file is listed; copy outlives the original; two messages of one thread; threads of one project; thread outside a project (scenarios) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:239,258,283,298,315` | TEST-CONFIRMED |
| Deleted draft in a project (scenario) | `T/ThreadAttachmentsTests/ThreadAttachmentsTests.cs:333` | TEST-CONFIRMED |
| **Privacy of the contents of a project** |  |  |
| Artifact, thread folder and file not sent of another member 403 to a member, the owner and a DocSpace admin (scenarios) | `T/ThreadFilesAccessTests/ThreadFilesStrangerAccessTests.cs:51,67,83,100,116,132` | TEST-CONFIRMED |
| Creator reads, downloads and copies (scenario) | `T/ThreadFilesAccessTests/ThreadFilesAuthorAccessTests.cs:41,52,63` | TEST-CONFIRMED |
| Creator cannot change the file (scenario); creator opens the editor (scenario) | `T/ThreadFilesAccessTests/ThreadFilesAuthorAccessTests.cs:78,92,107,132,146`; `T/ThreadFilesAccessTests/ThreadFilesAuthorAccessTests.cs:121` | TEST-CONFIRMED |
| **Used space of a project** |  |  |
| Used space of a project (scenario) | `T/ThreadFilesAccessTests/ThreadFilesDisclosureTests.cs:51`; `products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs:371-374` | TEST-CONFIRMED |
| Used space in a room webhook (scenario) | `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/WebhookEntryContextTests.cs:255`; `products/ASC.Files/Core/ApiModels/WebhookDto/FileEntryWebhookDtoHelper.cs` (`RestrictForSubscriberAsync`) | TEST-CONFIRMED [run: PASS] |
| Sorting by used space (scenario) | `T/ThreadFilesAccessTests/ThreadFilesDisclosureTests.cs:82`; `products/ASC.Files/Core/Utils/EntryManager.cs:1112,1255-1258` | TEST-CONFIRMED |
| **The contents of a project are not listed** |  |  |
| Listing; search inside the project (scenarios) | `T/ThreadFilesAccessTests/ThreadFilesProjectListingTests.cs:51,69,80` | TEST-CONFIRMED |
| Counters of the project; sources and skills (scenarios) | `T/ThreadFilesAccessTests/ThreadFilesProjectListingTests.cs:96,107` | TEST-CONFIRMED |
| **The rooms roots list nothing inside a project** |  |  |
| Listing the AI rooms root; filtering it by the title of a thread folder (scenarios) | `T/ThreadFilesAccessTests/ThreadFilesAiHubRootListingTests.cs:53,86,103`; `T/ThreadFilesAccessTests/ThreadFilesAiHubRootListingTests.cs:69` | TEST-CONFIRMED |
| Listing the rooms root across all sections (scenario) | `T/ThreadFilesAccessTests/ThreadFilesAiHubRootListingTests.cs:126` | TEST-CONFIRMED |
| **Recipients of an upload into a project** |  |  |
| Uploader is told; others are not told (scenarios) | `T/ThreadFilesAccessTests/ThreadFilesRecipientsTests.cs:52,68` | TEST-CONFIRMED |
| Owner's new items (scenario) | `T/ThreadFilesAccessTests/ThreadFilesRecipientsTests.cs:106` | TEST-CONFIRMED |
| Upload notification (scenario): the decision by room type | `CoreTests/DocSpaceHelperAnnouncementTests.cs:39,53` | TEST-CONFIRMED |
| Upload notification (scenario): the decision is applied where the upload event is published and where it is handled | `products/ASC.Files/Server/Api/UploadController.cs`; `products/ASC.Files/Core/HttpHandlers/ChunkedUploaderHandler.cs`; `products/ASC.Files/Server/Helpers/FilesHelperBase.cs`; `products/ASC.Files/Worker/IntegrationEvents/EventHandling/RoomNotifyIntegrationEventHandler.cs` (calls of `AnnouncesEntriesToMembers`) | CODE-ONLY |
| Shared users of a file (scenario) | `T/ThreadFilesAccessTests/ThreadFilesRecipientsTests.cs:91` | TEST-CONFIRMED |
| **Activity digests leave out AI projects** |  |  |
| Activity digests (scenario): the decision by room type | `CoreTests/DocSpaceHelperAnnouncementTests.cs:59,74` | TEST-CONFIRMED |
| Activity digests (scenario): the decision is applied to every digest event of a room | `products/ASC.Files/Core/Configuration/ProductEntryPoint.cs` (`GetAuditEventsAsync`, call of `ReportsActivityInDigest`) | CODE-ONLY |
| **Deleting a thread of a project** |  |  |
| Gone at once (scenario) | `T/ThreadDeletionTests/ThreadDeletionTests.cs:41,61` | TEST-CONFIRMED |
| Files removed by the background pass; other thread of the project; project deleted afterwards (scenarios) | `T/ThreadDeletionTests/ThreadDeletionTests.cs:90,111,135` | TEST-CONFIRMED |
| The pass runs every 5 minutes by default | `products/ASC.AI/Worker/BackgroundServices/DeletedThreadsCleanerService.cs` (`_defaultPeriod`) | CODE-ONLY |
| **Losing access to a project deletes the member's threads** |  |  |
| Member removed by the owner; member leaves; deactivated member removed (scenarios) | `T/ProjectMemberRemovalTests/ProjectMemberRemovalTests.cs:43,64,83` | TEST-CONFIRMED |
| Other members; invited again (scenarios) | `T/ProjectMemberRemovalTests/ProjectMemberRemovalTests.cs:105,125` | TEST-CONFIRMED |
| Owner and member remove the project from the list (scenarios) | `T/ProjectMemberRemovalTests/ProjectMemberRemovalTests.cs:148,172` | TEST-CONFIRMED |
| Owner changed (scenario) | `T/ProjectMemberRemovalTests/ProjectMemberRemovalTests.cs:192` | TEST-CONFIRMED |
| **Converting a member to Guest removes them from the projects** |  |  |
| Member converted; thread closed when the job completes (scenarios) | `T/GuestConversionTests/GuestConversionTests.cs:43,66` | TEST-CONFIRMED |
| Other members; owner of a project converted (scenarios) | `T/GuestConversionTests/GuestConversionTests.cs:85,105` | TEST-CONFIRMED |
| Member of an agent converted (scenario) | `T/GuestConversionTests/GuestConversionTests.cs:126` | TEST-CONFIRMED |
| **Thread list of an AI project** |  |  |
| Thread started in a project; thread started in a room (scenarios) | `T/ChatTests/ChatProjectThreadListTests.cs:41,62` | TEST-CONFIRMED |
| **Deleting a profile deletes its project threads** |  |  |
| Data of a member removed; owner of the project before the background pass (scenarios) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:43,62` | TEST-CONFIRMED |
| Member deleted at once; owner of the project deleted (scenarios) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:79,99` | TEST-CONFIRMED |
| Data transferred with the profile deleted (scenario) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:126` | TEST-CONFIRMED |
| **Transferring data keeps the contents of a project with their creator** |  |  |
| Member's data transferred to the project's owner (scenario) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:146` | TEST-CONFIRMED |
| **Transferring a project deletes its former owner's threads** |  |  |
| Owner's data transferred; members of the transferred project (scenarios) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:166,190` | TEST-CONFIRMED |
| Former owner's thread in another project (scenario) | `T/ProfileRemovalTests/ProfileRemovalTests.cs:213` | TEST-CONFIRMED |
| **Projects in the root folders** |  |  |
| Pinned projects come first; paging, filters and `sortBy` ignored; `total`/`count` 4, `startIndex` 0 (scenario) | `Hub/AiHubRootProjectsTests.cs:44` | TEST-CONFIRMED |
| Nothing pinned; more than five pinned (scenarios) | `Hub/AiHubRootProjectsTests.cs:70,90` | TEST-CONFIRMED |
| A changed project moves up (scenario) | `Hub/AiHubRootProjectsTests.cs:117` | TEST-CONFIRMED |
| No project in the section (scenario) | `Hub/AiHubRootProjectsTests.cs:137` | TEST-CONFIRMED |
| Unpinned project moves behind the pinned ones (scenario) | `Hub/AiHubRootProjectsTests.cs:154` | TEST-CONFIRMED |
| Other sections are not changed (scenario) | `Hub/AiHubRootProjectsTests.cs:173` | TEST-CONFIRMED |
| **Who finds a project in the root folders** |  |  |
| Member pins a project; pinned for the member only (scenario) | `Hub/AiHubRootProjectsTests.cs:195` | TEST-CONFIRMED |
| Member loses access and regains it (scenario) | `Hub/AiHubRootProjectsTests.cs:219` | TEST-CONFIRMED |
| DocSpace admin outside the project (scenario) | `Hub/AiHubRootProjectsTests.cs:252` | TEST-CONFIRMED |
| Creator of the project (scenario) | `Hub/AiHubRootProjectsTests.cs:273` | TEST-CONFIRMED |
| A share held through a group does not count | `products/ASC.Files/Core/Core/EF/Queries/FolderQueries.cs` (`AiHubRootProjectsAsync`, `s.Subject == userId`); no test: the API refuses a group on a project | CODE-ONLY |
| **Drafts that are not sent are removed** |  |  |
| Sent attachment older than the lifetime (scenario) | `T/UnsentAttachmentCleanupTests/UnsentAttachmentCleanupTests.cs:142` | TEST-CONFIRMED |
| The pass runs every 15 minutes and a draft lives 2 hours by default | `products/ASC.AI/Worker/BackgroundServices/OrphanAttachmentsCleanerService.cs` (`_defaultPeriod`, `_defaultLifetime`) | CODE-ONLY |
| A draft linked before the pass takes it keeps its file; a link after that leaves the draft unlinked | `products/ASC.Files/Core/Core/AI/AiProjectThreadsService.cs:125`; `common/ASC.AI.Integration/Database/AttachmentQueries.cs` (`MarkOrphanAttachmentsDeletedAsync`, `UpdateAttachmentBindingsByIdsAsync`); no test: the stand cannot order the link inside the pass | CODE-ONLY |
| **The file of a removed draft** |  |  |
| Deleted draft of a file in a project; abandoned draft of a file in a project (scenarios) | `T/UnsentAttachmentCleanupTests/UnsentAttachmentCleanupTests.cs:50,104` | TEST-CONFIRMED |
| Abandoned draft of a file outside a project (scenario) | `T/UnsentAttachmentCleanupTests/UnsentAttachmentCleanupTests.cs:126` | TEST-CONFIRMED |
| **Conversion of an AI agent into an AI project** (change `migrate-ai-agents-to-projects`; its tests passed on 2026-11-22 against the working tree, `--filter-namespace "*AgentMigrationTests*"`: 48 of 48, the unit classes `AiAgentsMigrationPlanTests`, `AiAgentMigrationTitleTests` and `AiAgentsMigrationConfigurationTests` 21 of 21, `TenantsModuleSpecificsTests` 4 of 4; proven red with mutation patches on the same day: the integration tests except `AgentMigrationRouteTests` `Migrate_PortalWithoutAgents_ReturnsZeros` and `Migrate_RepeatedCall_ReturnsZeros`, which no single removal turns red, and the unit classes except `AiAgentsMigrationConfigurationTests`, which was not tried; the agent tests of the Files suite, 219 of 219, passed with the pass off in the test profile) |  |  |
| Agent converted; conversion run twice (scenarios); an agent under the 4.0 agents root | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:55,97,79` | TEST-CONFIRMED |
| Pinned agent (scenario) | `T/AgentMigrationTests/AgentMigrationAccessTests.cs:248` | TEST-CONFIRMED |
| The logo of the agent is copied to the project; the logo files of the room of an agent without knowledge are deleted with the room, the copy made for the project stays | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:298,315` | TEST-CONFIRMED |
| The colour, the cover and the custom tags of the agent go to the project, the custom tags leave the Knowledge room | `T/AgentMigrationTests/AgentMigrationSettingsTests.cs:131` | TEST-CONFIRMED |
| The tool preferences, MCP servers and chat preferences scoped to the agent move to the project; the ones without an entity stay. Arranged and read through the internal storage API: the chat service stores them under no entity | `T/AgentMigrationTests/AgentMigrationSettingsTests.cs:56,83,107` | TEST-CONFIRMED |
| The New and Recent marks of the agent are dropped: none is left on the Knowledge room, none is moved to the project. No route marks a room as recent, so the test writes that mark by SQL | `T/AgentMigrationTests/AgentMigrationSettingsTests.cs:203` | TEST-CONFIRMED |
| **The room of a converted agent becomes a Knowledge room** |  |  |
| Knowledge kept; title at the limit; vectorization off (scenarios). The stand has no embedding provider, so a knowledge file is put into the Knowledge folder by SQL (`T/AgentMigrationTests/AgentMigrationTestBase.cs`, `AddKnowledgeFileAsync`) and every case runs with vectorization off | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:117,221,244`; the cut of the title: `CoreTests/AiAgentMigrationTitleTests.cs:42,48,56,64,75,86` | TEST-CONFIRMED |
| A folder inside the Knowledge folder is deleted with its files, the files lying directly in the Knowledge folder stay in the room. The test moves a folder of Chat outputs into the Knowledge folder by SQL (`T/AgentMigrationTests/AgentMigrationRoomTests.cs`, `MoveFolderToKnowledgeAsync`) | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:345` | TEST-CONFIRMED |
| A knowledge file is found by its title in the Knowledge room after the conversion: the index gets the change through an event or through its periodic pass, which reads the file by its new `modified_on`, so the test polls for 30 s and does not tell the two apart; the move sets `modified_on` of the current and the first version of a knowledge file to the time of the conversion (the pass finds a file by its first version), so the periodic pass of the index picks the file up when the event is lost; the conversion publishes the deletion of the Knowledge folder from the index (read off the event bus) | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:414,146,200` | TEST-CONFIRMED |
| **The Knowledge room of a converted agent drops the settings it refuses** |  |  |
| Restricted agent converted (scenario). `updateRoom` answers 403 when it turns indexing on for an agent (the reordering it starts walks into the Knowledge and Chat outputs folders, which are not rooms), so the test writes that flag by SQL, together with privacy and the two form settings, and reads the form settings by SQL | `T/AgentMigrationTests/AgentMigrationSettingsTests.cs:163` | TEST-CONFIRMED |
| **An agent without knowledge leaves no room** |  |  |
| Empty Knowledge folder (scenario) | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:275` | TEST-CONFIRMED |
| **Threads of an agent become threads of the project** |  |  |
| Thread follows the agent; thread started elsewhere (scenarios). The thread of the agent is created through the internal storage route with the agent as `entityId`: the public chat route stores an entity only for an AI project (`common/ASC.AI.Chat/app/storage/threadsStorage.ts:113`) | `T/AgentMigrationTests/AgentMigrationThreadsTests.cs:53,86`; thread of a Guest (scenario): `:165` | TEST-CONFIRMED |
| **The model of an agent becomes the model of the project** |  |  |
| Agent bound to a profile; agent without an assignment (scenarios) | `T/AgentMigrationTests/AgentMigrationThreadsTests.cs:111,138` | TEST-CONFIRMED |
| **Members of a converted agent** |  |  |
| Roles carried over; group invited to the agent; joining the group later (scenarios) | `T/AgentMigrationTests/AgentMigrationAccessTests.cs:51,72,98` | TEST-CONFIRMED |
| Guest of the agent (scenario) | `T/AgentMigrationTests/AgentMigrationAccessTests.cs:122` | TEST-CONFIRMED |
| A Guest among the members of an invited group is left out too: a group takes no Guest through the API, so no test builds the case | `products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs:851` (the local function `GetProjectMembersAsync` of `ConvertAiAgentAsync`, the Guest check on a member of a group) | CODE-ONLY |
| **Access to the Knowledge room of a converted agent** |  |  |
| Content creator keeps the room (scenario). That such a member can still upload is not asserted: the stand refuses every create in a Knowledge room while vectorization is off. A group that keeps the room is not built either: the API refuses a group as a Content creator of an agent (`products/ASC.Files/Core/Utils/FileSharing.cs:278-299`); the test asserts that a group invited as a Viewer is gone | `T/AgentMigrationTests/AgentMigrationAccessTests.cs:145` | TEST-CONFIRMED |
| Viewer reads through the project; Guest of the agent; external link of a knowledge file (scenarios) | `T/AgentMigrationTests/AgentMigrationAccessTests.cs:178,197,218` | TEST-CONFIRMED |
| **Chat outputs of a converted agent move to My documents** |  |  |
| Outputs moved; empty Chat outputs; owner over the quota; owner disabled (scenarios); a second call moves nothing | `T/AgentMigrationTests/AgentMigrationOutputsTests.cs:47,80,100,134,161` | TEST-CONFIRMED |
| The files of the moved Chat outputs folder, directly in it and in a folder inside it, get `modified_on` of their current and first version set to the time of the conversion, a version between keeps its date, and the folder inside it gets a new `modified_on` too, so the periodic pass of the full-text index stores their new ancestor chain when an index event is lost | `T/AgentMigrationTests/AgentMigrationOutputsTests.cs:188` | TEST-CONFIRMED |
| The conversion publishes an `UpdateFolders` index event for every file of the moved Chat outputs folder and for the folder inside it (read off the event bus) | `T/AgentMigrationTests/AgentMigrationOutputsTests.cs:263` | TEST-CONFIRMED |
| The new parent of the moved Chat outputs folder, the tree rows of its subtree and the `modified_on` of its files and subfolders are committed in one transaction, so a run that stops after the move leaves nothing for a later run to find: the stand cannot stop the process between the statements | `products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs` (`MoveFolderCoreAsync`, the `inTransaction` call before the commit); `products/ASC.Files/Core/Core/AI/AiAgentMigrator.cs` (`MoveOutputsAsync`, the delegate) | CODE-ONLY |
| No user's used space changes when the agent is moved under the AI rooms root | `T/AgentMigrationTests/AgentMigrationRoomTests.cs:451` | TEST-CONFIRMED |
| **The background pass converts agents by itself** |  |  |
| Restored archive (scenario): the restore writes the time of the restore into `last_modified` of the portal, which puts it into the window of the next pass. No integration test restores an archive: the backup suite never starts a restore (`common/Tests/ASC.Data.Backup.Tests/Tests/Restore/RestoreGuard.cs`) | `common/Tests/ASC.Data.Backup.Core.Tests/TenantsModuleSpecificsTests.cs:49`; `common/ASC.Data.Backup.Core/Tasks/Modules/AiModuleSpecifics.cs` (the `entry_id` relations) | CODE-ONLY, the time by a unit test |
| Portal being restored; unchanged portal (scenarios): the window and the candidate list are pure functions, the two queries select active portals only; the periodic loop is run by no integration suite, the pass is off in the test profile | `CoreTests/AiAgentsMigrationPlanTests.cs:44,50,56,86`; `common/ASC.Core.Common/EF/Queries/TenantQueries.cs:138,145`; `products/ASC.AI/Worker/BackgroundServices/AiAgentsMigrationService.cs:72,209` | CODE-ONLY, the decisions by unit tests |
| A pass stopped halfway (a restart, a deploy) resumes after the last portal it saved: the state is saved every 20 portals and once more when the pass ends by an exception or a cancellation (not when the process is killed), with the start of its first part, which opens the next window once it completes (no scenario; the periodic loop is run by no integration suite) | `CoreTests/AiAgentsMigrationPlanTests.cs:154,162,168,176,190`; `products/ASC.AI/Worker/BackgroundServices/AiAgentsMigrationService.cs:127,140,161` | CODE-ONLY, the decisions by unit tests |
| A portal whose conversion finds the lock of the legacy AI providers migration held elsewhere is skipped by that pass: no failed pass is counted, and the next pass takes it again whether it changed or not (no scenario; the stand cannot hold that lock from a test) | `CoreTests/AiAgentsMigrationPlanTests.cs:126,135,146`; `CoreTests/LegacyAiProvidersMigrationGateTests.cs:40`; `products/ASC.AI/Core/Migration/AiAgentsPortalMigration.cs:54`; `products/ASC.AI/Worker/BackgroundServices/AiAgentsMigrationService.cs:222` | CODE-ONLY, the decisions by unit tests |
| **A failed conversion is retried and then left** |  |  |
| One agent of two fails (scenario): each agent is converted in its own try/catch | `products/ASC.Files/Core/Core/AI/AiAgentMigrator.cs:65,75` | CODE-ONLY |
| Failure repeated; success after a failure (scenarios) | `CoreTests/AiAgentsMigrationPlanTests.cs:62,70,78,94,105,114`; the loop: `products/ASC.AI/Worker/BackgroundServices/AiAgentsMigrationService.cs:112,118` | CODE-ONLY, the decisions by unit tests |
| **Starting the conversion by request** |  |  |
| Owner starts the conversion; portal without agents (scenarios); a DocSpace admin is let in | `T/AgentMigrationTests/AgentMigrationRouteTests.cs:73,42,51,62` | TEST-CONFIRMED |
| Room admin refused; AI switched off (scenarios); no sign-in answers 401 | `T/AgentMigrationTests/AgentMigrationRouteTests.cs:89,112,102` | TEST-CONFIRMED |
| The route answers 409 while the lock of the legacy AI providers migration is held elsewhere (no scenario; the stand cannot hold that lock from a test) | `products/ASC.AI/Server/Api/AgentsMigrationController.cs` (`MigrateAgentsAsync`); the Busy decision of the lock: `CoreTests/LegacyAiProvidersMigrationGateTests.cs:40` | CODE-ONLY |

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| `aiSkillsGenerate` returns the skill for review and saves nothing | `770819a4fc`, `7efeb43643`; `portal-settings`, "Result of generating a skill" | CROSS-REFERENCE; `app/skills/__tests__/generateSkill.test.ts` asserts it (unit test, not run) |
| The provider's text on that route only for spending limit, rate limit, lack of credit; a description reaches the model as written | `72adbfaa14`; `skillsController.ts:90-103`; `portal-settings`, "Failures of generating a skill" | partly: "a description reaches the model as written" TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] (`app/skills/__tests__/generateSkill.test.ts:210`); the relay of the provider's text stays CODE-ONLY |
| Long threads compacted, on by default, summary stored as an ordinary message; off without a restart | `6fbbf9ecf3`; `aiController.ts` (`compaction`); `common/ASC.AI.Chat/README.md:191-208`; `app/__tests__/compactionConfig.test.ts` (settings only) | partly: the settings (on by default, off from the file or the environment) TEST-CONFIRMED [run: NO-LINK] [run: PASS, vitest 2026-11-10] (`app/__tests__/compactionConfig.test.ts:76,80,93,103`); the compaction itself stays CODE-ONLY - no .NET test drives one, and the folding is in the library |
| `context_length` becomes `contextWindow` in the gateway's model list; fallback to the id table, then a default; a saved profile keeps nothing | `303ac12711`; `profilesStorage.ts` (`mapGatewayModel`, `dtoToProfile`); README | CODE-ONLY |
| Cache breakpoints (`396266ebe9`, `cd37150ab8`) and library bumps | Internal to a round, no contract change | LEFT OUT |
| Chat and vectorization in a private room are denied | `bd89a24dcd` (`FileSecurity`); `private-rooms` | per diff; the answers of the chat routes there not traced, no spec text added |

## Where the server differs from the contract and the TypeScript expectations (spec follows the server)

| Document / test | Server |
|---|---|
| `T/McpTests/McpToolPermissionModeChatTests.cs:111` (BUG 84298, TS claim) expects a saved allow-always to win in `ask` | `Lib` `types.d.ts:503`: "under ask never". The previous draft said "unless pre-approved"; corrected. The test is red by code reading and its comment says the expectation is the TS claim. |
| Previous draft: `0` as a preference `entityId` answers 400 | `T/PreferenceTests/PreferenceEntityIdTests.cs:146`: empty and `0` are the user's own scope; corrected. |
| Previous draft: a thread in a project answers 403 for an outsider of any role, 409 without a model; a Member's round runs on the project's model | A Read member's round is not bound to the project (A16); the spec now states the outsiders a project refuses, the 409 on a round of a manager, and a manager's round. |
| `TS/chat/chat.permission.spec.ts:1567` (BUG 82719) expects 403 / 400 for a file id, the Trash, 0 and −1 | Reopen decision: any accessible folder is valid (`T/ChatTests/ChatPermissionValidationTests.cs:58-64`). |
| `TS/preferences/*` BUG 82900, 84306, 84418-84420 wanted a scope per room | Preferences have no scope; the .NET tests were rewritten to the one user value (`T/PreferenceTests/PreferenceScopeTests.cs`). |
| TS catalogue test title "publishes nothing", agent scopes for MCP | The registry is one map per portal (bug 82975); registered tools reach the model (`8b54d43b11`). |
| `T/TextToDocxTests/TextToDocxFolderIdTests.cs:84` vs TS 400 for a numeric string `folderId` | 404: a string is a third-party folder id. |
| TS `agents/*` and many `mcp`, `vectorization`, `billing` blocks | `test.skip(true, "AI agents removed in DocSpace 4.1.0")`: the agent routes are gone; the storage controller stays internal (`AI/Server/Api/AgentsController.cs`, test `T/AgentsTests/AgentListPermissionsTests.cs:51`). |
| yaml `aiApproveToolCall` 409 row names only `projectModelUnavailable` | The route also answers 409 for no pending call, an unknown message and an index out of range (`T/McpTests/McpToolCallDecisionTests.cs:70,91`). |

Contract re-check against yaml `5ea4dc6`:

| Item | Published? | Where |
|---|---|---|
| `GenerateMeta` | yes | enum at 69851 |
| `aiMetaGenerate`, `aiMetaFill` | yes | 5631, 5871 |
| `servicelimit/*` | yes | `createServiceLimit` 52223 and siblings |
| Optional `message` of approve / deny | yes | `AiApproveToolCallRequest` 71724, `message` not in `required` |
| `aiTools` build field, `GET api/2.0/settings/version/build` | **no** | no such path; `aiTools` at 106510 is the wallet flag of the tenant quota, another field |

## Left out of the spec, and why

- **The internal storage routes** (`/internal/ai/*`): the chat service's private contract. Tests such as
  `T/*StorageTests/` are cited only where they confirm a public behaviour.
- **The chat-context aggregate** (`T/ChatContextTests/`): implementation of a round.
- **`aiContextGetRoomSkill`**: the plumbing behind `read_skill`, whose effect is specified.
- **The legacy provider migration** (`T/MigrationTests/LegacyProviderMapperTests.cs`): runs once at startup.
- **Forms AI** (form analysis, field filling, `recommendedModelForForms`): the forms capability.
- **The DocSpace MCP server and the AI gateway**: separate deployables; only their effects are specified.
- **Rate-limit headers and 429 rows** of the yaml: generic policy.
- **Streaming frame shapes, `actionArgs.prompt.replace`, the context prompt**: the chat library's protocol.
- **`ai-reasoning`** (`T/ReasoningTests/`, `TS/ai-reasoning/`): model quality, except that the attachment reaches the model.
- **Image generation into rooms** (`T/ChatTests/ChatImageSaveTests.cs`, `ChatImageTests.cs:51,112`): chat tool behaviour not written up as a requirement.
- **Failure language, formula rendering, name normalisation of an export**: see A21–A23.
- **Client behaviour**: model picker, tours, activation screen.

## Revision 2026-11-09 (`bb406b409b..6fb013e745`)

No test was run. `Integration` is `common/ASC.AI.Integration/Profiles/`; `Gw` is `common/ASC.Core.Common/AI/AiGateway.cs`.

| Claim | Source | Status |
|---|---|---|
| `aiByokKeysList`, `aiByokProviders`, `aiByokKeysSet`, `aiByokKeysDelete` on `/api/2.0/ai/byok`, `/byok/providers`, `/byok/{id}` | yaml (tag `AI / BYOK`); `Chat/routes.ts:230-233`; `Chat/controllers/byokKeysController.ts`; `AI/Server/Api/ByokKeysController.cs` | CODE-ONLY |
| DocSpace admin only on all four; no gateway 404 | `AI/Server/Service/ProfileStorageService.cs` (`_writeTypes`, `AssertGatewayConfigured`); `T/ProfileTests/ByokRoutesWithoutGatewayTests.cs:67,88,99` | TEST-CONFIRMED [run: PASS] on the standalone stack: owner and DocSpace admin 404 (no gateway), Room admin, User and Guest 403 before it, anonymous 401 (`ByokRoutesWithoutGatewayTests.cs:67,88,99`); the admin-only check behind a configured gateway stays CODE-ONLY |
| Providers: chat and image models only, grouped by provider, models with a key left out, logo of the owner when there is one owner (`52319262fe`) | `ProfileStorageService.ReadByokProvidersAsync`; `Integration/ByokKeyType.cs` | CODE-ONLY |
| Keys list: revision id, name absent once the catalogue lost the model, provider, logo (`1b5e171fe3`); no key field (scenario "Stored key never read back") | `ProfileStorageService.ReadByokKeysAsync`; yaml `AiByokKey`; `Chat/storage/byokKeysStorage.ts:106-120` | CODE-ONLY |
| Set: 404 unknown or non-chat/image model, 400 provider not serving the model or key refused by the gateway's verification, upsert, audited (scenario "Key the provider rejects") | `ProfileStorageService.SetByokKeyAsync`; `Gw` (`VerifyByokKeyAsync`); `Integration/ByokKeyStorage.cs` (`SetAsync`); Chat relays the 400 message (`withValidationMessage`) | CODE-ONLY |
| Delete: 404 when no key, works with the catalogue unreachable, audit then names the id (`cc961ae3b4`, scenario "Delete without the catalogue") | `ProfileStorageService.DeleteByokKeyAsync`, `TryFindByokModelNameAsync` | CODE-ONLY |
| Embedding and web-search BYOK dropped; the key type (chat or image) is stored but nothing reads it yet (`bdde563b02`, `474bd45a2d`) | `Integration/ByokKeyType.cs`; `DbByokKey.Type` | CODE-ONLY; storage detail, not in spec |
| Gateway BYOK route `gateway/byok/{profileId}/{path}`: non-guest, the same allowed paths as the gateway proxy, token carries provider, revision id and key (`0572960719`) | `AI/Server/Api/AiGatewayProxyController.cs:75-89`; `Gw` (`GetByokKeyAsync`, `GenerateKeyAsync`) | CODE-ONLY; route hidden from the contract |
| Chat and the document agent call a BYOK profile through that route (`e6da90bb4a`) | `Chat/storage/profilesStorage.ts` (`onlyofficeGatewayPath`); `Chat/tools/documentAgent.ts`; `Chat/storage/__tests__/profilesStorage.test.ts:73,87` | TEST-CONFIRMED [run: NO-LINK] (unit test) |
| A model missing from the portal's offer joins through its key; a key whose provider no longer serves the model is ignored (`9e46c912b3`, scenario "Provider dropped the model") | `Integration/PortalModelsService.cs`; `Integration/ProfileReader.cs` (`ActiveByokProvider`) | CODE-ONLY |
| Undecryptable key: warning logged, no credentials, the BYOK route answers 404 (`94d7b02c21`); the catalogue still marks the profile as BYOK | `Integration/ByokKeyStorage.cs` (`DecryptKeyAsync`); `ProfileStorageService.ReadByokCredentialsAsync` | CODE-ONLY |
| A gateway profile carries `byokProvider` from the AI service; whether `aiProfilesList` passes it on is not traced; absent from yaml `AiProfile` | `AI/Server/Models/ResponseDto/ProfileDto.cs`; `Chat/storage/profilesStorage.ts` (`dtoToProfile`) | NOT TRACED |
| Wallet billing of BYOK usage (`ai-tools-byok`, `ai-search-byok`, BYOK marks in `getAiPrices`) | `billing` spec | CROSS-REFERENCE |
| Contract: `aiByokKeysDelete` answers the documented 200 with a success body (it answered 204 without a body until 2026-11-23); `aiByokKeysSet` documents 400 and 404, `aiByokKeysDelete` 404; list and providers document no 404 | yaml `/api/2.0/ai/byok/{id}`; `byokKeysController.ts` (`delete`); at unit level `byokKeysController.test.ts` | CODE-ONLY; spec follows the server |
| No .NET test covers a BYOK route | `products/ASC.AI/ASC.AI.Tests` (no `byok` match) | GAP |

## Suspected defects

Re-checked 2026-11-06. No test was run; "red by code reading" means the assertion was compared with the code.
On 2026-11-10 the tests of A16, A27, A28 and A30 were run (class by class) and are red as described;
A32 and A34 come from new tests of that run.

| # | Where | What the server does | Evidence | 2026-11-06 |
|---|-------|----------------------|----------|------------|
| A3 | Agent create | Room left behind when the binding failed. | route gone | FIXED by removal (`ecf76402b3`) |
| A4 | Default slot | Image-only Default handed to Chat. Default still takes it, resolve answers 404 and try-resolve null. | `T/AssignmentTests/AssignmentResolutionTests.cs:153,175`; `Chat/controllers/assignmentsController.ts:131-151,273-276` | FIXED (`d59923d4a0`) |
| A5 | Title regeneration | 500 on a real conversation. | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:102`; `T/ChatTests/ChatRegenerateTitleTests.cs:51` | FIXED (`876875d79d`, test comment), TS `test.fail` stale |
| A6 | Thread scope validation | File id, Trash accepted. | `T/ChatTests/ChatPermissionValidationTests.cs:58-64` | REFUTED: by design, reopen decision on bug 82719 — tests green, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) |
| A8 | MCP replace-all | Case twins stored. | `T/McpTests/McpServerReplaceAllTests.cs:227` (comment "used to") | FIXED (`b6d11f1999`, the library update) |
| A9 | Export drops the file | A form-filling room or a room out of quota answers 202 and writes nothing: the publisher checks the folder and the right only; the quota and the room rule meet the job later. | `AI/Core/MdTextToDocx/MdTextToDocxTaskQueue.cs:48-90`; `T/TextToDocxTests/TextToDocxQuotaTests.cs:51`, `TextToDocxRoomTypesTests.cs:73` | STILL OPEN, red by code reading — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bugs 83070, 83071; cause by code reading) |
| A10 | OpenAI passthrough bodies | 500 with a stack trace on a large body and malformed image requests. | commit message; `TS/openai-passthrough/openai-passthrough.spec.ts:404,497` keep old titles; no .NET test | FIXED (`9768f0a6f3`, `e84932692d`), by commit and code |
| A11 | Foreign prompt delete | `promptsStorage.delete` swallows the 404 and the controller answers success; the prompt survives. | `Chat/storage/promptsStorage.ts:144-153`; `Chat/controllers/promptsController.ts:319-328`; `T/PromptTests/PromptIsolationTests.cs:108` | STILL OPEN, red by code reading of those two files; the library engine between them was not read, and the test comment says "used to" — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 82809, `PromptIsolationTests.cs:108`) |
| A13 | Metadata gate | `assertAiAccess` borrows the gate through a throwaway tool-prefs read, on every route that reaches no gated AI endpoint first; the routes are the published operations of `Chat/__tests__/app.aiGate.test.ts` outside its `NOT_GATED_FIRST`. | `Chat/controllers/aiAccess.ts:51`; `Chat/__tests__/app.aiGate.test.ts` | STILL OPEN |
| A14 | Contract drift | See the contract table. | yaml `5ea4dc6` | FIXED except the `aiTools` build field and its route (STILL OPEN) |
| A16 | Read member of a project | A round is not bound to the project: chat context drops a folder without `CanUseAi` (Room manager or Content creator), and the fallback reads `type` from the folder DTO, null for a room. The round runs on what the composer sent, without the project's model and sources. | `Security:2742-2754`; `AI/Server/Service/ChatContextService.cs:161-176`; `Chat/storage/docspaceFilesApi.ts:354,681-685`; `products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs:158,407-417`; `T/ChatTests/ChatProjectModelTests.cs:129` | OPEN — red test, run 365d87fe60: `GetModel_ProjectMember_…` and `Send_OtherProfileInProject_…(asMember: True)`; the manager's case passes — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (cause by code reading) |
| A21 | Export names | `/` in a title is a path separator (`Notes 12/31` → `31.docx`); two simultaneous exports with one title get one name. | `T/TextToDocxTests/TextToDocxNamingTests.cs:118`, `TextToDocxConcurrencyTests.cs:68` (bugs 82711, 84459, "measured" in the comments) | NEW, per the tests, code not read — narrowed by run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`): the test of bug 82711 (`/`, `\` in an export title) is still red; the test of bug 84459 (two simultaneous exports with one title) is green |
| A22 | Export of formulas; failure language | TeX source lands in the document; a stored provider failure stays English under any culture. | `T/MessageStorageTests/MessageMarkdownDocxTests.cs:99` (83038); `T/ChatTests/ChatFailureLanguageTests.cs:81` (83046) | NEW, per the tests, code not read — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bugs 83038, 83046; code not read) |
| A23 | `aiThreadsOpenOrCreate` without a message | No `threadId`, no `firstMessage`: 500 from the engine. | `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:156` (82826) | NEW, per the test comment — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 82826) |
| A24 | Concurrent add of one name | Losers answer 403, not the soft "already registered". | `T/McpTests/McpServerConcurrencyTests.cs:62` | NEW, per the test comment — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) |
| A25 | Profile `createdAt` | The create answer has milliseconds no later read reproduces. | `T/ProfileStorageTests/ProfileReadTests.cs:47` | NEW, per the test comment — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (`ProfileReadTests.cs:47`) |
| A27 | Tool mode gates | An invalid value to the write route from a Guest or while AI is off answers 400: validation comes before the Guest and switch checks. | `ToolPermissionMode_Guest_CannotReadOrChangeIt` (84303), `ToolPermissionMode_AiDisabled_ReadAndWriteReturn403` (84302) | OPEN — red test, run 365d87fe60 — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bugs 84303, 84302) |
| A28 | Attachment cap | More attachments than the composer allows are answered 200 and stored whole on `aiThreadsAppendUserMessage` and the send (11 stored). | `AttachmentComposerRuleTests`, the two `MoreAttachmentsThanTheClientAllows` tests (82894) | OPEN — red test, run 365d87fe60 — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 82894) |
| A30 | Attachment to the model | A round with a draft by id, an inline record, a device file or an image ends healthy but no streamed request to the stub carries the text. Whether the stand's file reading plays a part was not traced. | `AttachmentSendTests`, four tests (82773) | OPEN — red test, run 365d87fe60 — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 82773; the stand's part was not traced) |
| A32 | Dropped provider connection | `aiProfilesListModels` on a saved profile whose provider accepts the connection and closes it without an answer is answered 200 with an empty model list instead of 502, so the form shows no models instead of the failure. | `T/ProfileTests/ProfileListModelsFailureTests.cs:67` (Category=Bug); `Chat/controllers/profilesController.ts` (`listModels`) | NEW 2026-11-10, red by run — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) |
| A34 | `withAiFolder` alone | The description of `getRoomAiFolder` says the rooms with skills are listed with `GET api/2.0/files/rooms?withAiFolder=true`, but without `searchArea=AiHub` the flag filters the Rooms section and the answer is empty; with `searchArea=AiHub` it lists the Instructions rooms only. | `products/ASC.Files/Tests/Tests/03_Rooms/AiHub/AiRoomOutsiderAccessTests.cs:107,129` (Category=Bug on 128); `FolderDao` (`withAiFolder`), `VirtualRoomsController.GetRoomAiFolder` remarks | NEW 2026-11-10, red by run — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (`AiRoomOutsiderAccessTests.cs:129`, `Category=Bug`) |
| A35 | `aiPromptsDelete` | Deleting another user's prompt answers `success` true although nothing is deleted (bug 82809): the route never reports whether a prompt of the caller was removed. The yaml describes it as intended. | `Chat/controllers/promptsController.ts:319-328`; `T/PromptTests/PromptIsolationTests.cs:108` (Trait Bug 82809) | NEW 2026-11-14, red by code reading — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 82809, `PromptIsolationTests.cs:108`) |
| A36 | Draft cleanup races a link | The pass read the files of unlinked drafts, then deleted them by id with no lock. A link that moved the file into the thread in between lost it: the message kept the attachment without the file. A link that read the file before the deletion and moved it after answered 500. Now the pass marks the unlinked drafts deleted first (`MarkOrphanAttachmentsDeletedAsync`, `MessageId == null && DeletedOn == null`), and both the file read and the row delete take marked drafts only; a link refuses a marked draft. | `products/ASC.Files/Core/Core/AI/AiProjectThreadsService.cs:125,128`; `products/ASC.Files/Core/Core/EF/Queries/AiIntegrationQueries.cs` (`ChatDraftFilesAsync`, `a.DeletedOn != null`); `common/ASC.AI.Integration/Database/AttachmentQueries.cs` (`MarkOrphanAttachmentsDeletedAsync`, `DeleteAttachmentsAsync`, `UpdateAttachmentBindingsByIdsAsync`) | FIXED `a1f6ce3988`, code read, not run; the interleaving has no test |
| A37 | Size after the deletion | Both passes return the size of the deleted files to the project after the deletion is committed; a failure of that step or a stop in between leaves the used space too high for good, as in every deletion path of Files (`FileDeleteOperation`). | `products/ASC.Files/Core/Core/AI/AiProjectThreadsService.cs:130-135,195-197`; `products/ASC.Files/Core/Services/WCFService/FileOperations/FileDeleteOperation.cs:905-984` | NEW 2026-11-20, red by code reading; left open on purpose |
