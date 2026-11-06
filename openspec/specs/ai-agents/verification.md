# ai-agents — verification

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

Revision 2026-11-06: re-checked against the merge of `release/v4.1.0` (`d58816bece..3533ad8a89`). The range removed the
agent routes (`ecf76402b3`), added Instructions and Knowledge rooms (`8fe15a629c`), AI projects, sources and their model
(`1c4190d327`, `ee2f480944`, `96a4fdefca`, `e10a553243`, `64c79db1dc`), the skills catalogue (`69e51f8e97`) and the
multi-room knowledge search (`10d18364af`, `3878ba506a`). The TS AI suite was ported to .NET (2300 cases). How rows were
checked: the assertion was read for the Category=Bug tests, the tests behind A1–A14 and the suspicious rows (about 40
tests); the other `file:line` citations come from the test index and its summaries, by test name. The spot checks found
two rows of the previous draft wrong (`0` as a preference scope, `ask` and a saved approval) and the name of
`Add_NameOfARegisteredServerWithoutConfig` read against the draft's "re-save"; a name may still mislead elsewhere.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Portal AI switch** | | |
| Read by every member, on by default | `Web/02_Settings/Common/AiAccessGetTests.cs:46,65`; `common/ASC.Core.Common/Tenants/TenantAiAccessSettings.cs:9-12` | TEST-CONFIRMED |
| Written by owner / DocSpace admin; others 403; anonymous 401; scenarios "turns AI off for everyone", "Room admin cannot flip" | `T/SettingsTests/SettingsAiAccessTests.cs:55,81,99,108` | TEST-CONFIRMED |
| Every AI route 403 while off, before the target is looked up | `T/ProfileTests/ProfileAiDisabledTests.cs:51,111`; `T/AssignmentTests/AssignmentAiDisabledTests.cs:46`; `T/ChatTests/ChatAiDisabledTests.cs:66,115`; `T/McpTests/McpToolsAiDisabledTests.cs:58`; `T/PromptTests/PromptAiDisabledTests.cs:50`; `T/ThreadStorageTests/ThreadsAiDisabledTests.cs:57` | TEST-CONFIRMED |
| AI rooms section hidden while off, back on restores (scenarios "AI rooms are hidden", "Switching back on") | `Hub/AiHubAiAccessTests.cs:45-174`; `T/SettingsTests/SettingsAiDisabledTests.cs:110,139` | TEST-CONFIRMED |
| Audited and pushed to clients | `web/ASC.Web.Api/Api/Settings/SettingsController.cs` (`ChangeAiAccessSettingsAsync`, audit message) | CODE-ONLY |
| **AI readiness reported to the portal** | | |
| Every role incl. Guest, same fields, anonymous 401; read while off; self-hosted `aiReady` needs a profile (scenario "Read while the switch is off") | `T/SettingsTests/SettingsGetTests.cs:77,91,102,133,153`; `T/SettingsTests/SettingsAiDisabledTests.cs:83,124` | TEST-CONFIRMED |
| Hosted: first three true only while on and paid (scenario "Unpaid hosted portal") | `TS/settings/settings.ai-disabled.spec.ts:174`; `AI/Core/Settings/AiSettingsService.cs` | TS-ONLY |
| **The AI Tools wallet service gates inference only** | | |
| Send and regenerate 402, nothing stored (scenario "Send refused while unpaid") | `TS/chat/chat.ai-disabled.spec.ts:352`; `Chat/controllers/aiController.ts` | TS-ONLY (TS block skipped) |
| Gateway proxy paths 403 while unpaid | `AI/Server/Api/AiGatewayProxyController.cs` (`AiServiceDisabledException`) | CODE-ONLY |
| Management surface stays open (scenario); low balance does not stop inference | `TS/chat/chat.ai-disabled.spec.ts:488,553`; `TS/mcp/mcp.ai-disabled.spec.ts` | TS-ONLY (TS block skipped) |
| **AI tools version of the editors** | | |
| `aiTools` next to the editors' version, null when off or old (scenario) | `web/ASC.Web.Api/Core/BuildVersion.cs:55,83`; `products/ASC.Files/Core/Helpers/DocumentService.cs` | CODE-ONLY |
| **Per-user AI chat settings** | | |
| Own value, true for new user, every role, while off (scenario "One user's preference is private") | `T/SettingsTests/SettingsUserConfigTests.cs:46,64,110,223`; `SettingsUserConfigPermissionsTests.cs:51,70,88`; `SettingsAiDisabledTests.cs:98,139` | TEST-CONFIRMED |
| Non-boolean, empty, missing, non-object 400; unknown field ignored; 200 means stored (scenario "Non-boolean value") | `T/SettingsTests/SettingsUserConfigBodyTests.cs:79,96,113,125,137,157` | TEST-CONFIRMED |
| **Tool permission mode of a user** | | |
| Three modes, auto default, both read pairs, other preference leaves it (scenario "Modes round-trip") | `T/McpTests/McpToolPermissionModeSettingsTests.cs:65,76,127`; `T/PreferenceTests/PreferencePermissionTests.cs:162` | TEST-CONFIRMED |
| Outside the three 400 (scenario "Value outside the three"); per user (scenario "Modes are per user") | `T/McpTests/McpToolPermissionModeSettingsTests.cs:89,105` | TEST-CONFIRMED |
| Steers the pause; next tool call (scenario "Mode steers the approval pause") | `T/McpTests/McpToolPermissionModeChatTests.cs:91,121,137,153,166,200` | TEST-CONFIRMED |
| `ask` ignores a saved pre-approval (scenario "Pre-approval in ask") | `Lib/dist/core/services/ai/types.d.ts:503` ("under ask never"); `T/McpTests/McpToolPermissionModeChatTests.cs:111` asserts the opposite and is red by code reading (see differences) | CODE-ONLY |
| **Gates on the tool permission mode routes** | | |
| Guest 403 on all four, invalid value from Guest 403 (scenario "Guest on the two read routes") | `T/PreferenceTests/PreferencePermissionTests.cs:198` (bug 84303; line 212 reads the `aiSettingsGetUser` default) | TEST-CONFIRMED |
| AI off 403 first, mode survives | `T/PreferenceTests/PreferenceAiDisabledTests.cs:76` (bug 84302) | TEST-CONFIRMED |
| **Profile catalogue** | | |
| Identical description to reading roles, get-by-id has no key or headers | `T/ProfileTests/ProfileCatalogueTests.cs:56,96,133,178`; `ProfileAccessReadTests.cs:91` | TEST-CONFIRMED |
| Guest 403, anonymous 401, malformed / empty 400, unknown 404 (scenarios "Guest cannot read", "Unknown id") | `T/ProfileTests/ProfileAccessGuestTests.cs:47,58`; `ProfileAccessReadTests.cs:52`; `ProfileIdBoundaryTests.cs:46,54,73` | TEST-CONFIRMED |
| List carries the key to an administrator (scenario) | `T/ProfileTests/ProfileAccessReadTests.cs:108`; every member too, see A15 | TEST-CONFIRMED |
| Hosted catalogue: gateway list, no embedding, restricted leaves (scenario "Restricted model leaves") | `TS/profiles/profiles.spec.ts:1477,1513`; `AI/Server/Service/ProfileStorageService.cs` | TS-ONLY |
| **Models offered by a stored profile's provider** | | |
| Missing / malformed 400, unknown 404, every reading role, lists models | `T/ProfileTests/ProfileIdBoundaryTests.cs:85,93,113,128`; `ProfileAccessReadTests.cs:153` | TEST-CONFIRMED |
| Guest and AI off refused before the dial (scenario) | `T/ProfileTests/ProfileAccessGuestTests.cs:82`; `ProfileAiDisabledTests.cs:73` | TEST-CONFIRMED |
| Key rejected 400, no answer 502 | yaml `aiProfilesListModels`; `T/ProfileTests/ProfileProviderReachabilityTests.cs:45` (discovery sibling) | CODE-ONLY |
| **Managing profiles** | | |
| DocSpace admin only; others 403; anonymous 401 before validation; AI off first (scenario "Room admin cannot delete") | `T/ProfileTests/ProfileWriteGuardTests.cs:110,132`; `ProfileAccessReadTests.cs:73`; `ProfileAiDisabledTests.cs:152,173`; `T/ProfileStorageTests/ProfileDeleteTests.cs:62,79` | TEST-CONFIRMED |
| Round trip (scenario "Self-hosted admin round-trips") | `T/ProfileStorageTests/ProfileCreateTests.cs:43`; `ProfileUpdateTests.cs:41`; `ProfileDeleteTests.cs:41`; `T/ProfileTests/ProfileCatalogueTests.cs:56` | TEST-CONFIRMED |
| Unusable profile refused before connecting (scenario) | `T/ProfileTests/ProfileWriteGuardTests.cs:50,67,84`; `ProfileProviderTypeTests.cs:110,138,152` | TEST-CONFIRMED |
| Hosted: every write 403 (scenario "Owner on a hosted portal") | `AI/Server/Service/ProfileStorageService.cs` (`AssertGatewayNotConfigured`); TS `profiles/profiles.spec.ts` hosted block | TS-ONLY |
| Base URL ≤ 2048, key ≤ 4096; audited | `AI/Server/Models/RequestDto/AiIntegrationLimits.cs`; `ProfileStorageService.cs` (`AiProfileCreated/Updated/Deleted`) | CODE-ONLY |
| **Provider discovery** | | |
| Required fields named, empty body, malformed URL, bad key header 400 | `T/ProfileTests/ProfileProviderValidationTests.cs:63,85,104,118,144,164` | TEST-CONFIRMED |
| Private address, scheme refused before dialling (scenario) | `T/ProfileTests/ProfileProviderUrlGuardTests.cs:56,74,100`; `ProfileProviderReachabilityTests.cs:87` | TEST-CONFIRMED |
| Wrong key (scenario); 502; no echo, nothing stored | `T/ProfileTests/ProfileProviderModelsTests.cs:134,149`; `ProfileProviderReachabilityTests.cs:45`; `ProfileProviderValidationTests.cs:191` | TEST-CONFIRMED |
| Local types (scenario "Local provider"), every model named; Guest and AI off | `T/ProfileTests/ProfileProviderReachabilityTests.cs:60,72`; `ProfileProviderModelsTests.cs:104`; `ProfileAccessGuestTests.cs:103`; `ProfileAiDisabledTests.cs:90` | TEST-CONFIRMED |
| **Connection check of a stored profile** | | |
| Bare id or wrapper; missing / malformed 400; unknown 404; flat verdict (scenario) | `T/ProfileTests/ProfileTestConnectionTests.cs:47,58,74,107,118,128` | TEST-CONFIRMED |
| Same verdict per role; Guest 403, malformed id before the role check | `T/ProfileTests/ProfileAccessReadTests.cs:153`; `ProfileAccessGuestTests.cs:69,119,136` | TEST-CONFIRMED |
| **Model restrictions and prices of a hosted portal** | | |
| Null entry 400 before the action (scenario "Null entry"); valid body reaches the 403 of the test portal | `Web/04_Portal/Payments/PaymentAiModelRestrictionsValidationTests.cs:55,73,89,107` | TEST-CONFIRMED |
| `getAiPrices`, `getRestrictedAiModels`, the rest of `setRestrictedAiModels` | `Payments` (three actions); yaml 53405-53546 at `baf28cf`, `getAiPrices` etc. at 5ea4dc6 | CODE-ONLY |
| Restriction reaches an open conversation (scenario) | `TS/messages/messages.spec.ts`, `TS/agents/agents.spec.ts` restriction blocks | TS-ONLY (TS block skipped) |
| **Tasks and the scope of the assignments** | | |
| The eleven tasks | `common/ASC.AI.Integration/Assignments/ActionType.cs`; yaml 69851 (`GenerateMeta`); `T/AssignmentTests/AssignmentAssignTests.cs:100` | TEST-CONFIRMED |
| Unknown scope 404 (scenario); unopenable 403; Room is the portal map (scenario) | `T/AssignmentTests/AssignmentEntityScopeTests.cs:51,67,81`; `T/ChatTests/ChatRoomModelTests.cs:46,122`; `T/AssignmentsStorageTests/AssignmentsReadTests.cs:108,123` | TEST-CONFIRMED |
| Unknown task 400 | `T/AssignmentTests/AssignmentReadTests.cs:85`; `AssignmentRemoveTests.cs:61` | TEST-CONFIRMED |
| **Who may read and write assignments** | | |
| Reads for three roles, Guest 403, anonymous 401, AI off 403 (scenario "Guest cannot read") | `T/AssignmentTests/AssignmentPermissionTests.cs:46,80`; `AssignmentAiDisabledTests.cs:46` | TEST-CONFIRMED |
| Writes DocSpace-admin only; refused writes nothing (scenario "Room admin cannot bind") | `T/AssignmentTests/AssignmentPermissionTests.cs:134`; `AssignmentAiDisabledTests.cs:73` | TEST-CONFIRMED |
| **Resolving the model of a task** | | |
| Own binding, then Default; FormAnalysis independent (scenario "Unassigned falls back") | `T/AssignmentTests/AssignmentReadTests.cs:68`; `AssignmentResolutionTests.cs:51,72,87,109` | TEST-CONFIRMED |
| 404 not 500; try-resolve 200 null; image default (scenarios "Default holds an image model", "Chat and a profile that cannot chat") | `T/AssignmentTests/AssignmentResolutionTests.cs:121,139,153,175` (84450 asserts not-500; the 404 is Chat `assignmentsController.ts:80-100`) | TEST-CONFIRMED |
| `expand` `profile` with totals | `T/AssignmentTests/AssignmentEntityScopeTests.cs:91` | TEST-CONFIRMED |
| Hosted defaults (scenario) | `TS/assignments/assignments.spec.ts:33` (two bindings); `AI/Server/Service/AssignmentsResolver.cs:11-32` (FormAnalysis) | TS-ONLY |
| **Writing assignments** | | |
| Capability check; unknown profile refused (scenario); missing field 400 | `T/AssignmentTests/AssignmentCapabilityTests.cs:64`; `AssignmentAssignTests.cs:151,169` | TEST-CONFIRMED |
| Bulk all or nothing (scenario), empty map (scenario) | `T/AssignmentTests/AssignmentBulkTests.cs:42,62,87,99,122,149` | TEST-CONFIRMED |
| Unassign, cascade | `T/AssignmentTests/AssignmentRemoveTests.cs:42,61,75,102` | TEST-CONFIRMED |
| Audited (scenario) | `AI/Server/Service/AssignmentsStorageService.cs` (`AiProfileAssigned`, `AiProfileUnassigned`) | CODE-ONLY |
| **The AI rooms section** | | |
| Created by owner / admin / Room admin, User 403; section root (scenario "Section listing") | `Hub/AiHubRootTests.cs:48,63,124`; `Hub/AiProjectRoomTests.cs:46,63,77,87,101,132,164` | TEST-CONFIRMED |
| Settings that do not apply 400 (scenario) | `Hub/AiHubRoomSettingsTests.cs:47,58,69,80,93,106` | TEST-CONFIRMED |
| Never archived, no template, no folder in the root (scenario) | `Hub/AiHubRootTests.cs:79,94,109`; `Hub/AiProjectRoomTests.cs:196,210` | TEST-CONFIRMED |
| Knowledge room needs vectorization 403 | `Security:1422,1441`; `AI/Core/Settings` | CODE-ONLY |
| **Members of the AI rooms** | | |
| Guest not in a project (scenario); User joins through a link as Member | `Hub/AiProjectGuestInviteTests.cs:46,62,82`; `Hub/AiProjectInvitationLinkTests.cs:45` | TEST-CONFIRMED |
| Member reads, manager writes (scenario); non-member 403 | `Hub/AiProjectModelTests.cs:60,77,95,124`; `Hub/AiProjectRoomTests.cs:116,148,179` | TEST-CONFIRMED |
| Role sets of Instructions and Knowledge rooms | `Security` (`GetAccesses`); `products/ASC.Files/Core/Helpers` | CODE-ONLY |
| DocSpace admin outside a project cannot read model, sources or skills | `Hub/AiProjectSourcesLinkTests.cs:248`, `Hub/AiRoomSkillsTests.cs:118` cover a User; admin by `Security` | CODE-ONLY |
| **The model of an AI project** | | |
| Null while none selected; Instructions / unknown 404 | `Hub/AiProjectModelTests.cs:46,111,185` | TEST-CONFIRMED |
| Manager only; unknown, empty 400 (scenario "Selection cannot be cleared") | `Hub/AiProjectModelTests.cs:124,141,155,168` | TEST-CONFIRMED |
| Image profile refused (scenario) | `T/ChatTests/ChatProjectModelTests.cs:238,255` | TEST-CONFIRMED |
| Member reads the fixed model (scenario) | `Hub/AiProjectModelTests.cs:60`; `T/ChatTests/ChatProjectModelTests.cs:129` (the read half; the round half is A16) | TEST-CONFIRMED |
| Model moved (scenario) | `T/ChatTests/ChatProjectModelTests.cs:94` | TEST-CONFIRMED |
| Audited, pushed | `products/ASC.Files/Server/Api/VirtualRoomsController.cs` (`setProjectModel`) | CODE-ONLY |
| **Sources of an AI project** | | |
| Sorted; duplicates once (scenario) | `Hub/AiProjectSourcesLinkTests.cs:42,56,77` | TEST-CONFIRMED |
| Refused whole: other type, unknown, deleted, empty, over 40, unreadable, Viewer (scenario "Room not linkable") | `Hub/AiProjectSourcesLinkTests.cs:94,111,125,138,153,166,179,197,217` | TEST-CONFIRMED |
| Over the limit (scenario); deleted room (scenario); unlink | `Hub/AiProjectSourcesUnlinkTests.cs:42,60,113,129` | TEST-CONFIRMED |
| Manager only | `Hub/AiProjectSourcesLinkTests.cs:231`; `Hub/AiProjectSourcesUnlinkTests.cs:73,91` | TEST-CONFIRMED |
| Audited and pushed | `products/ASC.Files/Core` (share and socket calls in the link service) | CODE-ONLY |
| **Access through a project** | | |
| Member and group read linked room; non-member 403; not listed in members | `Hub/AiProjectMemberAccessTests.cs:45,67,85,160` | TEST-CONFIRMED |
| Unlink / removal ends it (scenario); own role not lowered | `Hub/AiProjectMemberAccessTests.cs:103,122,140` | TEST-CONFIRMED |
| Skills for a member | `Hub/AiProjectMemberAccessTests.cs:177`; `Hub/AiRoomSkillsTests.cs:62` | TEST-CONFIRMED |
| **Skills of an Instructions room** | | |
| Front matter rules, upload, chunked, new version (scenario "Invalid name") | `Instr/SkillUploadTests.cs:41-221` | TEST-CONFIRMED |
| Copy and move refused; no folder | `Instr/SkillMoveCopyTests.cs:58,90,107`; `Instr/InstructionsRoomFolderTests.cs:41,56,75,91` | TEST-CONFIRMED |
| Skill follows its file (scenario) | `Hub/RoomsSkillsLifecycleTests.cs:41,58,73,88,105,124,139,155`; `Instr/SkillMoveCopyTests.cs:123,157` | TEST-CONFIRMED |
| **Listing the skills** | | |
| `getRoomSkills` | `Hub/AiRoomSkillsTests.cs:46,62,82,95,108,118,134` | TEST-CONFIRMED |
| `getRoomsSkills` (scenario "Unreadable room left out") | `Hub/RoomsSkillsTests.cs:41,61,76,94,107,120,137,147` | TEST-CONFIRMED |
| `getRoomAiFolder` (scenario "Room without a skill") | `T/SkillTests/SkillFolderListingTests.cs:48,58,69` | TEST-CONFIRMED |
| `getRoomAiFolder` non-reader 403; `withAiFolder` (scenario) | `products/ASC.Files/Server/Api/VirtualRoomsController.cs`; yaml 30261 | CODE-ONLY |
| **Using the skills in a chat** | | |
| Index and `read_skill`; body only on request (scenario "Model reads one skill") | `T/SkillTests/SkillChatTests.cs:46,73,93` | TEST-CONFIRMED |
| Member yes, outsider, Guest, revoked no (scenario "Outsider") | `T/SkillTests/SkillPermissionTests.cs:80,98,138,153,183,209` | TEST-CONFIRMED |
| Composer lists the rooms (scenario) | `Chat/controllers/contextController.ts`; yaml `aiContextGetContextFolders`, `aiContextGetRoomSkills` | CODE-ONLY |
| **An AI agent as a room: the deltas** | | |
| Roles, Guest Viewer only (scenario "Guest as Content creator") | `T/AttachmentTests/AttachmentDeviceAccessTests.cs:77,115`; `T/ChatTests/ChatPermissionGuestTests.cs:186` | TEST-CONFIRMED |
| Root holds no files; Viewer, Content creator (scenario "Upload to the agent root") | `T/AttachmentTests/AttachmentDestinationTests.cs:107,152`; `AttachmentDeviceAccessTests.cs:46,97` | TEST-CONFIRMED |
| Knowledge: no subfolders | `T/AttachmentTests/AttachmentDestinationTests.cs:127` | TEST-CONFIRMED |
| Knowledge: indexable formats only | `TS/vectorization/vectorization.validation.spec.ts:144`; `Security` | TS-ONLY (TS block skipped) |
| Imposes no model (scenario); stored prompt (scenario) | `T/ChatTests/ChatAiRoomTests.cs:46`; `T/ChatTests/ChatComposerAgentTests.cs:53,108` | TEST-CONFIRMED |
| No template (scenario) | `TS/agents/agents.spec.ts` template block | TS-ONLY (TS block skipped) |
| **Starting a chat thread** | | |
| Roles; Guest 403 | `T/ChatTests/ChatStartTests.cs:49`; `ChatPermissionGuestTests.cs:93`; `ChatPermissionAnonymousTests.cs:74` | TEST-CONFIRMED |
| Blank title (scenario), no body | `T/ThreadStorageTests/ThreadsCreateTests.cs:102,114,131`; `T/ChatTests/ChatFirstMessageTests.cs:78,93` | TEST-CONFIRMED |
| Limit (scenario); parallel (scenario); verbatim | `T/ThreadStorageTests/ThreadTitleLengthTests.cs:43,57`; `ThreadsCreateTests.cs:89,199` | TEST-CONFIRMED |
| `profileId` malformed 400, unknown 404; no profile (scenario) | `T/ChatTests/ChatThreadModelRulesTests.cs:84,144`; `T/ThreadStorageTests/ThreadCreateTests.cs:98` | TEST-CONFIRMED |
| Listed empty until a message; creator alone | `T/ChatTests/ChatFirstMessageTests.cs:49`; `ChatGlobalEntryTests.cs:89` | TEST-CONFIRMED |
| **Where a thread may be started** | | |
| Omitted / null ordinary; malformed and unknown 404; any accessible folder | `T/ThreadStorageTests/ThreadsCreateTests.cs:143,170,184`; `T/ChatTests/ChatPermissionValidationTests.cs:64` | TEST-CONFIRMED |
| Empty or 0 as ordinary | `Chat/storage/docspaceFilesApi.ts:472-478` (empty); 0 by `IntegrationServiceBase` | CODE-ONLY |
| Non-member (scenario); Read member (scenario) | `T/ChatTests/ChatPermissionNonMemberTests.cs:57,64,79,104`; `ChatPermissionRevokedTests.cs:52` | TEST-CONFIRMED |
| Outsider of a project (scenario) | `T/ChatTests/ChatProjectAccessTests.cs:48` | TEST-CONFIRMED |
| **Thread privacy** | | |
| Creator only on every route (scenario "Two members") | `T/ThreadStorageTests/ThreadsPermissionTests.cs:67`; `ThreadsLifecycleTests.cs:209`; `T/ChatTests/ChatPermissionIsolationTests.cs:77`; `ChatPermissionIsolationSendTests.cs:51` | TEST-CONFIRMED |
| Removed member (scenario) | `T/ChatTests/ChatPermissionRevokedTests.cs:52,67,90` | TEST-CONFIRMED |
| Regeneration by a fellow member (scenario) | `T/MessageStorageTests/MessageRegenerateAccessTests.cs:55,79` | TEST-CONFIRMED |
| **One thread list per user** | | |
| Rooms share one list (scenario); third-party | `T/ChatTests/ChatEntityContextTests.cs:71,108,139`; `ChatGlobalEntryTests.cs:146`; `T/ThirdPartyTests/ThirdPartyThreadTests.cs:68` (bug 82855, by design) | TEST-CONFIRMED |
| Unopenable room lists the same | `T/ChatTests/ChatRoomAcrossUsersTests.cs:47`; `T/ThirdPartyTests/ThirdPartyThreadTests.cs:88` | TEST-CONFIRMED |
| Room deleted (scenario) | `T/ChatTests/ChatRoomGoneTests.cs:45,86` | TEST-CONFIRMED |
| **Listing, paging and searching threads** | | |
| Newest first, `count`, 0 → 400, deleted leaves | `T/ChatTests/ChatThreadListingTests.cs:46,73`; `T/ThreadStorageTests/ThreadsListTests.cs:46,73,92` | TEST-CONFIRMED |
| Cursor (scenario); search (scenario) | `T/ThreadStorageTests/ThreadsListTests.cs:102,137`; `ThreadReadTests.cs:151`; `ThreadSearchTests.cs:53,74` | TEST-CONFIRMED |
| Touch moves only that thread, never back | `T/ChatTests/ChatThreadManagementTests.cs:46`; `T/ThreadStorageTests/ThreadsTouchTests.cs:72` | TEST-CONFIRMED |
| **Renaming, deleting and clearing a thread** | | |
| Rename verbatim, refused body, unknown | `T/ThreadStorageTests/ThreadsRenameTests.cs:57,97,119,137`; `T/ChatTests/ChatThreadValidationTests.cs:54` | TEST-CONFIRMED |
| Delete; gone everywhere (scenario) | `T/ThreadStorageTests/ThreadsDeleteClearTests.cs:48,75,88`; `T/ChatTests/ChatThreadValidationTests.cs:90,111` | TEST-CONFIRMED |
| Clear; cleared carries on (scenario) | `T/ThreadStorageTests/ThreadsDeleteClearTests.cs:100,144`; `T/ChatTests/ChatClearedThreadTests.cs:50`; `ChatThreadValidationTests.cs:140` | TEST-CONFIRMED |
| Rename mid-reply (scenario) | `T/ChatTests/ChatMutatedWhileWritingTests.cs:57` | TEST-CONFIRMED |
| Malformed id 400 | `T/ChatTests/ChatThreadValidationTests.cs:76` | TEST-CONFIRMED |
| **Touching and reopening a thread** | | |
| Touch | `T/ThreadStorageTests/ThreadsTouchTests.cs:49,91,107` | TEST-CONFIRMED |
| Replay; another profile (scenario); no profile object 404 | `T/ChatTests/ChatOpenOrCreateTests.cs:67,104,137,168`; `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:47,108` | TEST-CONFIRMED |
| Opened by the first message (scenario) | `T/ChatTests/ChatOpenOrCreateTests.cs:198`; `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:132` | TEST-CONFIRMED |
| **Regenerating a thread title** | | |
| Refused request (scenario) | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:52,78`; `T/ChatTests/ChatRegenerateTitleTests.cs:73` | TEST-CONFIRMED |
| Title from a conversation (scenario), repeat | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:102`; `T/ChatTests/ChatRegenerateTitleTests.cs:51` | TEST-CONFIRMED |
| Foreign 403, AI off | `T/ThreadStorageTests/ThreadsPermissionTests.cs:67`; `ThreadsAiDisabledTests.cs:57` | TEST-CONFIRMED |
| **Messages of a thread** | | |
| Append verbatim, no answer; parallel (scenario) | `T/MessageStorageTests/ThreadMessagesAppendTests.cs:43,70,82,122` | TEST-CONFIRMED |
| Refusals; assistant by hand (scenario) | `T/MessageStorageTests/ThreadMessagesAppendValidationTests.cs:51,75,103,124,145,165` | TEST-CONFIRMED |
| Too many attachments (scenario) | `Chat/controllers/_helpers.ts:44-72`, `threadsController.ts:253`, `aiController.ts:780`; `T/AttachmentTests/AttachmentComposerRuleTests.cs:91,108,129` (accept a refusal or trim, status < 500) | TEST-CONFIRMED |
| Reading one message (scenario) | `T/MessageStorageTests/ThreadMessagesGetByIdTests.cs:46,70,101`; `MessageReadOneTests.cs:46` | TEST-CONFIRMED |
| Update in place (scenario "Edited question") | `T/MessageStorageTests/ThreadMessagesUpdateTests.cs:43`; `ThreadMessagesUpdateValidationTests.cs:52,103,131`; `MessageRewriteQuestionTests.cs:49,95` | TEST-CONFIRMED |
| Deleting a message (scenario) | `T/MessageStorageTests/ThreadMessagesDeleteTests.cs:99,118` | TEST-CONFIRMED |
| Another user's message 403 | `T/MessageStorageTests/MessageCrossUserTests.cs:46,66` | TEST-CONFIRMED |
| **Reading the history** | | |
| Oldest first, count, 400, 404, missing id | `T/MessageStorageTests/ThreadMessagesReadTests.cs:47,100,118,132,158` | TEST-CONFIRMED |
| Paging back (scenario); cursor (scenario); bare id | `T/MessageStorageTests/MessageReadTests.cs:153,192`; `MessagePagingTests.cs:51` | TEST-CONFIRMED |
| Raw markdown, growing snapshots | `T/MessageStorageTests/MessageCopyTests.cs:47,78`; `MessageMarkdownReplyTests.cs:47,143` | TEST-CONFIRMED |
| **Sending a message with a streamed reply** | | |
| Stored, NDJSON; first send (scenario) | `T/ChatTests/ChatSendTests.cs:52`; `ChatReplyStreamTests.cs:55`; `ChatFirstMessageTests.cs:114,164` | TEST-CONFIRMED |
| Context stays inside (scenario) | `T/ChatTests/ChatSendTests.cs:72,102` | TEST-CONFIRMED |
| Hang-up (scenario); provider refuses (scenario) | `T/MessageStorageTests/MessageStopTests.cs:58,91`; `T/ChatTests/ChatProviderFailureTests.cs:51` | TEST-CONFIRMED |
| **Refusals of a send** | | |
| Empty, unknown thread, foreign, Guest, AI off | `T/ChatTests/ChatPermissionValidationTests.cs:81,98`; `ChatPermissionIsolationSendTests.cs:51`; `ChatPermissionGuestTests.cs:118`; `ChatAiDisabledTests.cs:115` | TEST-CONFIRMED |
| Unknown profile (scenario), malformed | `T/ChatTests/ChatThreadModelRulesTests.cs:168`; `ChatHiddenFailureTests.cs:46` | TEST-CONFIRMED |
| Non-member of project; oversized (scenario) | `T/ChatTests/ChatProjectAccessTests.cs:84`; `ChatHiddenFailureTests.cs:77` | TEST-CONFIRMED |
| Unpaid 402 | `TS/chat/chat.ai-disabled.spec.ts:352` | TS-ONLY (TS block skipped) |
| Project without a usable model 409 (scenario) | `Chat/storage/aiProjectChatContext.ts:216-250`; `Chat/storage/__tests__/aiProjectChatContext.test.ts`; yaml 743 ff. | CODE-ONLY |
| **The model of a chat** | | |
| Project answers on its model (scenario); blank, unknown | `T/ChatTests/ChatProjectModelTests.cs:47,74,186,212,278` | TEST-CONFIRMED |
| Thread created in a project (scenario) | `T/ChatTests/ChatProjectModelTests.cs:157,314` | TEST-CONFIRMED |
| Thread's own, Chat, Default; rebinding moves nothing | `T/ChatTests/ChatThreadModelTests.cs:50,99,198`; `ChatThreadModelRulesTests.cs:49,113,183`; `ChatOtherActorTests.cs:45`; `ChatRoomModelTests.cs:87` | TEST-CONFIRMED |
| Picker switches (scenario) | `T/ChatTests/ChatThreadModelTests.cs:129,165` | TEST-CONFIRMED |
| Image-only (scenario) | `T/ChatTests/ChatImageTests.cs:84` | TEST-CONFIRMED |
| **One-shot sends** | | |
| No thread, no history (scenario) | `T/MessageStorageTests/MessageOneShotSendTests.cs:49,189`; `MessageOneShotSendCustomTests.cs:137` | TEST-CONFIRMED |
| Empty, malformed, Default, entity rules, `systemPrompt`, `isStream` | `T/MessageStorageTests/MessageOneShotSendTests.cs:89,116,144,160`; `MessageOneShotSendCustomTests.cs:46,80,95` | TEST-CONFIRMED |
| Guest 403 | `T/ChatTests/ChatPermissionGuestTests.cs:118` | TEST-CONFIRMED |
| **OpenAI-shaped send** | | |
| Stream (scenario), persist, model, hang-up | `T/MessageStorageTests/MessageOpenAiStreamTests.cs:45`; `MessageOpenAiStreamThreadTests.cs:47,74,106,165` | TEST-CONFIRMED |
| Tool call (scenario); failure inside the stream | `T/MessageStorageTests/MessageOpenAiStreamTests.cs:73,149` | TEST-CONFIRMED |
| **Regenerating a reply** | | |
| Last reply replaced; multi-turn (scenario) | `T/MessageStorageTests/MessageRegenerateTests.cs:48,117,160` | TEST-CONFIRMED |
| Thread's model, project's model | `T/MessageStorageTests/MessageRegenerateModelTests.cs:50`; `MessageRegenerateProjectTests.cs:47` | TEST-CONFIRMED |
| Tool chain whole; abandoned (scenario) | `T/McpTests/McpToolCallRegenerateTests.cs:71,107,139`; `T/MessageStorageTests/MessageStopTests.cs:130` | TEST-CONFIRMED |
| Nothing to regenerate, unknown, foreign, AI off | `T/MessageStorageTests/MessageRegenerateTests.cs:87,104`; `MessageRegenerateAccessTests.cs:55,107` | TEST-CONFIRMED |
| **Tool calls and the approval pause** | | |
| Pause, approve string / structured, deny (scenario "Deny") | `T/McpTests/McpToolCallPauseTests.cs:54,89,123,140` | TEST-CONFIRMED |
| Two-hop (scenario) | `T/McpTests/McpToolCallPauseTests.cs:165` | TEST-CONFIRMED |
| Announcement (scenario) | `T/McpTests/McpToolCallAnnouncementTests.cs:81,112,138,156` | TEST-CONFIRMED |
| Opt-out and pre-approved | `T/McpTests/McpAllowAlwaysPauseTests.cs:66,80,92` | TEST-CONFIRMED |
| Unavailable, withdrawn, server left; switched off (scenario) | `T/McpTests/McpHostToolsChatTests.cs:115,184,210`; `McpRegisteredServerChatTests.cs:137,160` | TEST-CONFIRMED |
| **Decisions on a tool call** | | |
| Nothing to decide (scenario), unknown, out of range 409 | `T/McpTests/McpToolCallDecisionTests.cs:70,91` | TEST-CONFIRMED |
| Anonymous, non-member, AI off | `T/McpTests/McpToolCallDecisionTests.cs:103,120,143` | TEST-CONFIRMED |
| Replay (scenarios "Replayed approval", "Replayed denial"), stale | `T/McpTests/McpToolCallReplayTests.cs:63,76,89,106,123` | TEST-CONFIRMED |
| `message` snapshot ignored | `common/ASC.AI.Chat/scripts/schema/schemaTypes.ts:215-245`; yaml 71724 (`message` optional) | CODE-ONLY |
| **Deep mode and reasoning level** | | |
| Off and unset; on, off, clear; on after off (scenario) | `T/PreferenceTests/PreferenceDeepModeStateTests.cs:46-120` | TEST-CONFIRMED |
| Levels | `T/PreferenceTests/PreferenceReasoningLevelTests.cs:47,55,70` | TEST-CONFIRMED |
| One user's value (scenario) | `T/PreferenceTests/PreferencePermissionTests.cs:92,133` | TEST-CONFIRMED |
| Reasoning reaches the client (scenario) | `T/PreferenceTests/PreferenceDeepModeChatTests.cs:87,119` | TEST-CONFIRMED |
| **Scope and refusals of the preferences** | | |
| Room is no scope (scenario); unopenable, unknown | `T/PreferenceTests/PreferenceScopeTests.cs:52,66,83,100,116,126`; `PreferencePermissionTests.cs:111` | TEST-CONFIRMED |
| Thread as scope (scenario); malformed; empty and 0 | `T/PreferenceTests/PreferenceEntityIdTests.cs:73,90,106,126,146` | TEST-CONFIRMED |
| Non-boolean, empty, bad depth | `T/PreferenceTests/PreferenceDeepModeValidationTests.cs:49,62,84`; `PreferenceReasoningLevelTests.cs:89`; `PreferenceClearTests.cs:52` | TEST-CONFIRMED |
| Guest, anonymous, AI off | `T/PreferenceTests/PreferencePermissionTests.cs:47,78`; `PreferenceAiDisabledTests.cs:57` | TEST-CONFIRMED |
| **Custom MCP servers** | | |
| Roles (scenario "Member cannot register") | `T/McpTests/McpToolsManagePermissionsTests.cs:46`; `McpToolsMemberPermissionsTests.cs:46`; `McpToolsAccessTests.cs:138`; `McpToolsAnonymousTests.cs:58` | TEST-CONFIRMED |
| Round trip (scenario), no dial, verbatim, flag (scenario) | `T/McpTests/McpServerRegistryTests.cs:50,93,135,349,366` | TEST-CONFIRMED |
| Unregistered name (scenario) | `T/McpTests/McpServerRegistryTests.cs:260` | TEST-CONFIRMED |
| AI off (scenario), off period (scenario) | `T/McpTests/McpToolsAiDisabledTests.cs:58,82`; `McpToolsAccessTests.cs:286` | TEST-CONFIRMED |
| Audited | `AI/Server/Service/McpServerStorageService.cs` (`ServerCreated/Updated/Deleted`) | CODE-ONLY |
| **One registry per portal** | | |
| Registered from a room (scenario); folder | `T/McpTests/McpServerEntityScopeTests.cs:49,66,77,97`; `T/ThirdPartyTests/ThirdPartyToolsTests.cs:68,80` (skips on the stand) | TEST-CONFIRMED |
| Unknown room (scenario), deleted, unopenable | `T/McpTests/McpServerEntityScopeTests.cs:160,190`; `McpToolsAccessTests.cs:208` | TEST-CONFIRMED |
| Reads ignore the entity (scenario) | `T/McpTests/McpServerEntityScopeTests.cs:146` | TEST-CONFIRMED |
| Name without config | `T/McpTests/McpServerRegistryTests.cs:236,247` | TEST-CONFIRMED |
| **Server names** | | |
| Limit (scenario); hard refusals; blank | `T/McpTests/McpServerNameTests.cs:59,83,94,242`; `McpServerReplaceAllTests.cs:200` | TEST-CONFIRMED |
| Underscore (scenario); replace-all per entry | `T/McpTests/McpServerNameTests.cs:137,148` | TEST-CONFIRMED |
| Unicode (scenario) | `T/McpTests/McpServerNameTests.cs:206,223` | TEST-CONFIRMED |
| Casing, whitespace; soft refusals (scenario); removal (scenario) | `T/McpTests/McpServerRegistryTests.cs:170,184,195,209,222,305,332` | TEST-CONFIRMED |
| **Server configurations and replace-all** | | |
| Both url and command (scenario); wrong type; empty; missing | `T/McpTests/McpServerConfigValidationTests.cs:56,67,79,110,124,142,164` | TEST-CONFIRMED |
| Unusable map (scenario) | `T/McpTests/McpServerReplaceAllTests.cs:93,107,121,134` | TEST-CONFIRMED |
| Exact map; empty; one bad entry (scenario); case twins (scenario) | `T/McpTests/McpServerReplaceAllTests.cs:50,81,145,162,227` | TEST-CONFIRMED |
| **Switching tools off** | | |
| Disable, re-enable (scenario), replacement, read routes | `T/McpTests/McpDisabledToolsTests.cs:49,64,109,133,248,258,267` | TEST-CONFIRMED |
| Server type and list refusals (scenario "Refused list") | `T/McpTests/McpDisabledToolsTests.cs:164,186,201,217,233` | TEST-CONFIRMED |
| Per user | `T/McpTests/McpToolPrefsLifecycleTests.cs:146`; `McpToolsSettingsPermissionsTests.cs:46` | TEST-CONFIRMED |
| Dropped server (scenario) | `T/McpTests/McpToolPrefsLifecycleTests.cs:45,81`; `T/McpServerStorageTests/McpServerReplaceTests.cs:139` | TEST-CONFIRMED |
| Guest, AI off naming a server (scenario) | `T/McpTests/McpToolsAccessTests.cs:318,338` | TEST-CONFIRMED |
| Portal-wide list for everyone (`scope` `tenant`) | `Chat/controllers/toolsController.ts` (`parseToolPrefsScope`, bug 84381) | CODE-ONLY |
| **Pre-approving tools** | | |
| Flat tokens, idempotent, revoke, falsy, string, missing | `T/McpTests/McpAllowAlwaysTests.cs:46,57,67,127,144,158,173,186` | TEST-CONFIRMED |
| One user's approval (scenario); independent of disabled | `T/McpTests/McpToolPrefsLifecycleTests.cs:101,128` | TEST-CONFIRMED |
| Approval with `allowAlways` (scenario) | `T/McpTests/McpAllowAlwaysPauseTests.cs:92,122,136`; `McpRegisteredServerChatTests.cs:116` | TEST-CONFIRMED |
| Same name on another server (scenario) | `T/McpTests/McpAllowAlwaysTests.cs:96,110` | TEST-CONFIRMED |
| **Tools catalogue** | | |
| Groups, no twin, every role, Guest, AI off, entity | `T/McpTests/McpToolsCatalogueTests.cs:47,59,91,107,125,137`; `McpToolsAiDisabledTests.cs:82` | TEST-CONFIRMED |
| Unreachable server (scenario) | `T/McpTests/McpToolsCatalogueTests.cs:74`; `McpRegisteredServerChatTests.cs:95` | TEST-CONFIRMED |
| Registered tools reach the model | `T/McpTests/McpRegisteredServerChatTests.cs:85` | TEST-CONFIRMED |
| **Web-search configuration on a self-hosted portal** | | |
| Probe; wrong key (scenario); unreachable; malformed; business 400 | `T/WebSearchTests/WebSearchConfigureTests.cs:46,62,78,95,112` | TEST-CONFIRMED |
| `aiWebSearchSetActiveConfig`; refused writes (scenario) | `T/WebSearchTests/WebSearchSetActiveConfigTests.cs:62,75,94,108,118,131`; `WebSearchConfigureTests.cs:121,135` | TEST-CONFIRMED |
| Guest with a malformed body (scenario); AI off first | `T/WebSearchTests/WebSearchSetActiveConfigTests.cs:148,168`; `WebSearchConfigureTests.cs:154,170` | TEST-CONFIRMED |
| Audited | `AI/Server/Service/WebSearchStorageService.cs` | CODE-ONLY |
| **Clearing and the hosted add-on** | | |
| Room admin cannot clear (scenario); admin clears; unconfigured; entity body (scenario) | `T/WebSearchTests/WebSearchPermissionsTests.cs:100,112,125`; `WebSearchStateTests.cs:90,102,123` | TEST-CONFIRMED |
| Clearing audited (scenario) | `AI/Server/Service/WebSearchStorageService.cs` | CODE-ONLY |
| Add-on, every scope, tool (hosted) | `TS/web-search/web-search.spec.ts:128,624`; `T/WebSearchTests/WebSearchChatToolTests.cs:52` (tool only while configured) | TS-ONLY |
| Owner cannot clear the add-on config (scenario); writes 403 | `TS/web-search/web-search.spec.ts:345`; `TS/web-search/web-search.config.spec.ts:57` | TS-ONLY |
| **Reading and probing the web-search configuration** | | |
| Roles (scenario "Guest cannot read"), anonymous, entity rules | `T/WebSearchTests/WebSearchPermissionsTests.cs:68,79`; `T/SettingsTests/SettingsWebSearchPermissionsTests.cs:52,79,104`; `WebSearchStateTests.cs:144,155,171` | TEST-CONFIRMED |
| Off period (scenario) | `T/WebSearchTests/WebSearchPermissionsTests.cs:142` | TEST-CONFIRMED |
| Probe; Guest and off dial nothing | `T/WebSearchTests/WebSearchTestConnectionTests.cs:70,89,102,152`; `WebSearchPermissionsTests.cs:167,181,200` | TEST-CONFIRMED |
| Private base URL (scenario), everywhere (scenario) | `T/WebSearchTests/WebSearchEgressGuardTests.cs:80,92,109`; `WebSearchBaseUrlFormatTests.cs:69` | TEST-CONFIRMED |
| **Web-search passthrough** | | |
| Relay, query and key (scenario); roles; no provider (scenario) | `T/WebSearchTests/WebSearchPassthroughTests.cs:66,75,92,104,129,191`; `WebSearchPassthroughAccessTests.cs:54,80` | TEST-CONFIRMED |
| Wrong shape 400; 4xx relayed; 5xx and unreachable 502 | `T/WebSearchTests/WebSearchPassthroughValidationTests.cs:84,101,117`; `WebSearchPassthroughTests.cs:149,164,178` | TEST-CONFIRMED |
| Attributed to a document (scenario) | `T/WebSearchTests/WebSearchPassthroughAccessTests.cs:99` | TEST-CONFIRMED |
| **OpenAI-compatible passthrough** | | |
| Relay; every optional field (scenario); stream; image on a text model | `TS/openai-passthrough/openai-passthrough.spec.ts:58,109,143` | TS-ONLY |
| Guest (scenario), anonymous, AI off | `TS/openai-passthrough/openai-passthrough.permission.spec.ts:37,58`; `openai-passthrough.ai-disabled.spec.ts:15,46` | TS-ONLY |
| Gateway proxy path list | `AI/Server/Api/AiGatewayProxyController.cs` | CODE-ONLY |
| **Editor tools catalogue** | | |
| Names, schema, approval; repeated reads | `T/EditorToolTests/EditorToolListTests.cs:51,64,102,111,149,165,186` | TEST-CONFIRMED |
| Guest catalogue (scenario); non-guest same; anonymous; AI off | `T/EditorToolTests/EditorToolPermissionTests.cs:49,63,163`; `EditorToolAiDisabledTests.cs:53` | TEST-CONFIRMED (skips on the stand) |
| **Calling an editor tool** | | |
| Name rules, argument rules | `T/EditorToolTests/EditorToolCallTests.cs:79,89,104,122`; `EditorToolArgumentTests.cs:45,71,102` | TEST-CONFIRMED |
| Guest; AI off (scenario) | `T/EditorToolTests/EditorToolPermissionTests.cs:101,113,137`; `EditorToolAiDisabledTests.cs:66,88,104,116` | TEST-CONFIRMED |
| Tool never exceeds the member (scenario); failure inside `result` | `T/EditorToolTests/EditorToolRestToolTests.cs:92,167` | TEST-CONFIRMED (skips on the stand) |
| Generator in a room (scenario); no write (scenario); entity names nothing (scenario) | `T/EditorToolTests/EditorToolEntityIdTests.cs:78,93,110,127,156,182` | TEST-CONFIRMED (skips on the stand) |
| **Creating chat attachments** | | |
| Attach by id (scenario), large, picture | `T/AttachmentTests/AttachmentStoredFileTests.cs:80,177,218`; `AttachmentSaveFileTests.cs:46,178` | TEST-CONFIRMED |
| `path` refusals; no text; archive (scenario) | `T/AttachmentTests/AttachmentSaveFileValidationTests.cs:100,112,184`; `AttachmentSaveFileTests.cs:211`; `AttachmentStoredFileTests.cs:106,136,153,252`; `AttachmentComposerRuleTests.cs:58,72` | TEST-CONFIRMED |
| Guest (scenario), anonymous, AI off | `T/AttachmentTests/AttachmentPermissionTests.cs:63,103`; `AttachmentAiDisabledTests.cs:150,162,178` | TEST-CONFIRMED |
| Batch (scenarios "Batch with one bad element", "Batch") | `T/AttachmentTests/AttachmentSaveFilesManyTests.cs:45,97,141,214,230`; `AttachmentCrossUserTests.cs:236` | TEST-CONFIRMED |
| Fields stored as sent (scenario) | `T/AttachmentTests/AttachmentSaveFileTypeTests.cs:47,75`; `AttachmentSaveFileValidationTests.cs:155`; `AttachmentSaveFileTests.cs:115` | TEST-CONFIRMED |
| **Reading and deleting attachments** | | |
| Another user's draft (scenario); mixed delete (scenario) | `T/AttachmentTests/AttachmentCrossUserTests.cs:57,77,186,209` | TEST-CONFIRMED |
| Unknown, malformed, empty, missing body | `T/AttachmentTests/AttachmentGetManyTests.cs:45,74,86,119`; `AttachmentGetTests.cs:72,85,106`; `AttachmentDeleteTests.cs:71,103,131` | TEST-CONFIRMED |
| Immediately readable, gone; source kept | `T/AttachmentTests/AttachmentGetManyTests.cs:130,151`; `AttachmentDeleteTests.cs:85,172` | TEST-CONFIRMED |
| Draft reaches the model (scenario) | `T/AttachmentTests/AttachmentSendTests.cs:68,91`; `T/ReasoningTests/ReasoningAttachTests.cs:48` | TEST-CONFIRMED |
| Unpaid portal (scenario) | `TS/attachments/attachments.ai-disabled.spec.ts` | TS-ONLY |
| **Linking attachments to a message** | | |
| Dies with the message (scenario), thread | `T/AttachmentTests/AttachmentLinkToMessageTests.cs:68,89,199,218` | TEST-CONFIRMED |
| 400 / 404 / 403 refusals; foreign draft | `T/AttachmentTests/AttachmentLinkToMessageTests.cs:111,122,133,145,156,166,177,190`; `AttachmentCrossUserTests.cs:139,265` | TEST-CONFIRMED |
| **Suggested questions for a form attachment** | | |
| 200 status and list; unknown (scenario); foreign; bad id; anonymous; AI off | `T/AttachmentTests/AttachmentSuggestedQuestionsTests.cs:63,87,98,106,119,133`; `AttachmentAiDisabledTests.cs:225` | TEST-CONFIRMED |
| **Exporting markdown into a document** | | |
| 202 no id, rendered | `T/TextToDocxTests/TextToDocxExportTests.cs:69,91,158`; `T/MessageStorageTests/MessageMarkdownDocxTests.cs:46` | TEST-CONFIRMED |
| Format (scenario "Unknown format") | `T/TextToDocxTests/TextToDocxFormatTests.cs:70,86`; `TextToDocxShapesTests.cs:62` | TEST-CONFIRMED |
| Required; whitespace (scenario); folder refusals | `T/TextToDocxTests/TextToDocxValidationTests.cs:49,75,91`; `TextToDocxFolderIdTests.cs:50,67,84,98,116` | TEST-CONFIRMED |
| Size limit (scenario) | `T/TextToDocxTests/TextToDocxSizeLimitTests.cs:52,71,101,125` | TEST-CONFIRMED |
| Before the switch | `T/MessageStorageTests/MessagesAiDisabledTests.cs:59,85,104` | TEST-CONFIRMED |
| Into Chat outputs (scenario) | `T/TextToDocxTests/TextToDocxThreadExportTests.cs:115` | TEST-CONFIRMED |
| Unpaid portal (scenario) | `TS/messages/messages.ai-disabled.spec.ts` | TS-ONLY |
| **Where an export may land** | | |
| Author; right to create; Viewer (scenario) | `T/MessageStorageTests/MessagesPermissionRoomAccessTests.cs:52,79,103,128`; `MessagesPermissionForeignFolderTests.cs:46,61,77`; `MessagesPermissionOwnFolderTests.cs:55,73` | TEST-CONFIRMED |
| Archive, Trash | `T/TextToDocxTests/TextToDocxTargetsTests.cs:47,62,85` | TEST-CONFIRMED |
| Same title (scenario); truncation; cleared; simultaneous | `T/TextToDocxTests/TextToDocxNamingTests.cs:47,74`; `TextToDocxThreadExportTests.cs:231`; `TextToDocxConcurrencyTests.cs:100` | TEST-CONFIRMED |
| **Vectorization of knowledge** | | |
| AI off 403, missing file 404 | `T/SettingsTests/SettingsAiDisabledTests.cs:66` | TEST-CONFIRMED |
| File in My documents (scenario), list rules, Guest, repeats, no handle | `TS/vectorization/vectorization.validation.spec.ts:58,95`; `products/ASC.Files/Core/Vectorization/VectorizationTaskPublisher.cs:55-90`; `Security:681-691,2693-2706` | TS-ONLY (TS block skipped) |
| Status and socket; Knowledge room rights (scenario) | `Security:1428-1447`; `VectorizationTaskPublisher.cs` | CODE-ONLY |
| Knowledge room without vectorization (scenario) | `Security:1441`; room creation in `products/ASC.Files/Core/Core/FileStorageService.cs` | CODE-ONLY |
| **Searching knowledge in a chat** | | |
| One query, room filter, 20-room cap (scenario) | `products/ASC.AI/Core/Knowledge/KnowledgeSearchEngine.cs:46-75` (`roomIds.Contains`); `Chat/storage/aiProjectChatContext.ts` | CODE-ONLY |
| Tool offered only while enabled; `aiContextGetKnowledgeFolders` | `Chat/controllers/contextController.ts`; yaml 1806 | CODE-ONLY |
| **Vectorization settings** | | |
| Self-hosted roles (scenario "Room admin"), anonymous, reset, body (scenario) | `T/SettingsTests/SettingsVectorizationPermissionsTests.cs:58,80,100,116,135,146` | TEST-CONFIRMED |
| Hosted 403 (scenario) | `TS/settings/settings.permission.spec.ts:439,512` | TS-ONLY |
| Key verified; audited | `AI/Core/Settings/AiSettingsService.cs` | CODE-ONLY |
| **Prompt library** | | |
| Roles | `T/PromptTests/PromptRoleTests.cs:49,71`; `PromptGuestTests.cs:47`; `PromptAnonymousTests.cs:46`; `PromptAiDisabledTests.cs:50` | TEST-CONFIRMED |
| Private (scenario "Libraries are private"); foreign prompt (scenario); foreign folder (scenario) | `T/PromptTests/PromptIsolationTests.cs:54,74,136,157,172,231,260` | TEST-CONFIRMED |
| Folder read by id (scenario); folder delete cascades | `T/PromptTests/PromptFolderLifecycleTests.cs:109,130,177`; `PromptIntegrityTests.cs:215` | TEST-CONFIRMED |
| Operations | `T/PromptTests/PromptLifecycleTests.cs:47,68,91`; `PromptMoveTests.cs:47`; `PromptExportTests.cs:49`; `PromptReadAndListTests.cs:47,176` | TEST-CONFIRMED |
| **Prompt names, texts and bundles** | | |
| Name limit (scenario), not a string, rename | `T/PromptStorageTests/PromptNameLengthTests.cs:43,58`; `T/PromptFolderStorageTests/PromptFolderNameLengthTests.cs:43,61`; `T/PromptTests/PromptFolderLifecycleTests.cs:231`; `PromptCreateValidationTests.cs:53` | TEST-CONFIRMED |
| Unique, case-insensitive; same name elsewhere (scenario); recheck (scenario) | `T/PromptTests/PromptCreateValidationTests.cs:127,149`; `PromptLifecycleTests.cs:154`; `PromptMoveTests.cs:129,159,185` | TEST-CONFIRMED |
| Text | `T/PromptTests/PromptLifecycleTests.cs:141,204`; `PromptTextSizeTests.cs:114,128` | TEST-CONFIRMED |
| Import modes (scenario); refused bundle (scenario) | `T/PromptTests/PromptImportTests.cs:81,108,129`; `PromptImportRoundTripTests.cs:98,119`; `PromptImportValidationTests.cs:94,120,136,154,169,181`; `PromptImportRefusalTests.cs:67,85,104` | TEST-CONFIRMED |
| **Metadata fields proposed for a file** | | |
| Route rules, trim, dedupe, 50, nothing written (scenario `kind` `folder`) | `Chat/controllers/metaController.ts`; `Chat/meta/generateMeta.ts`; `Chat/meta/__tests__/generateMeta.test.ts`; yaml 5631 | CODE-ONLY |
| **Metadata values filled from a file** | | |
| `dryRun`, `overwrite`, per-field report (scenario) | `Chat/meta/fillMeta.ts`; `Chat/meta/__tests__/fillMeta.test.ts`; yaml 5871 | CODE-ONLY |
| **The model and the failures of the metadata routes** | | |
| GenerateMeta else Default, 503; 402, 429, 502, 504 (scenario) | `Chat/meta/common.ts`; `Chat/meta/__tests__/common.test.ts`; `Chat/controllers/__tests__/metaController.test.ts` | CODE-ONLY |
| **Billing of AI usage on a hosted portal** | | |
| Charges; source (scenario); off stops | `TS/billing/billing.spec.ts:145,255,350,437` | TS-ONLY (TS block skipped) |
| **Spending limits on the AI services** | | |
| Scope left out (scenario) | `Web/04_Portal/Payments/PaymentServiceLimitValidationTests.cs:54,73` | TEST-CONFIRMED |
| Service layer: owner, cache, read, errors | `CoreTests/TariffServiceServiceLimit{Cache,Owner,Read}Tests.cs`; `CoreTests/AccountingClientServiceLimit{Error,Request}Tests.cs` | TEST-CONFIRMED |
| Portal limit reached (scenario), personal wins | `TS/limits/limits.enforcement.spec.ts:191,287`; `TS/limits/limits.per-user.spec.ts` | TS-ONLY |
| Roles, 403, 404, 409, audit (scenario) | `Payments` (nine `*ServiceLimit*` actions); `TS/limits/limits.crud.spec.ts`; yaml 52223 | CODE-ONLY |
| **Audit trail and real-time events of AI changes** | | |
| AI access, profile, assign, server, vectorization, web search, user settings, project, quota | `SettingsController.cs`; `ProfileStorageService.cs`; `AssignmentsStorageService.cs`; `McpServerStorageService.cs`; `AiSettingsService.cs`; `WebSearchStorageService.cs` | CODE-ONLY |
| Switch audited (scenario) | `SettingsController.cs` (`MessageAction`); `Web/05_Security/AuditTrail/` tests the audit API only | CODE-ONLY |
| Reads not audited (scenario); socket events | absence of a message in read paths; `AI/Server/Service/AiSocketManager.cs:46-52` | CODE-ONLY |

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| `aiSkillsGenerate` returns the skill for review and saves nothing | `770819a4fc`, `7efeb43643`; `portal-settings`, "Result of generating a skill" | CROSS-REFERENCE; `app/skills/__tests__/generateSkill.test.ts` asserts it (unit test, not run) |
| The provider's text on that route only for spending limit, rate limit, lack of credit; a description reaches the model as written | `72adbfaa14`; `skillsController.ts:72-83`; `portal-settings`, "Failures of generating a skill" | CODE-ONLY; the metadata routes keep the old relay, see A26 |
| Long threads compacted, on by default, summary stored as an ordinary message; off without a restart | `6fbbf9ecf3`; `aiController.ts` (`compaction`); `common/ASC.AI.Chat/README.md:33-50`; `app/__tests__/compactionConfig.test.ts` (settings only) | CODE-ONLY; no .NET test drives a compaction, and the folding itself is in the library, not read |
| `context_length` becomes `contextWindow` in the gateway's model list; fallback to the id table, then a default; a saved profile keeps nothing | `303ac12711`; `profilesStorage.ts` (`mapGatewayModel`, `dtoToProfile`); README | CODE-ONLY |
| Cache breakpoints (`396266ebe9`, `cd37150ab8`) and library bumps | Internal to a round, no contract change | LEFT OUT |
| Chat and vectorization in a private room are denied | `bd89a24dcd` (`FileSecurity`); `private-rooms` | per diff; the answers of the chat routes there not traced, no spec text added |

## Where the server differs from the contract and the TypeScript expectations (spec follows the server)

| Document / test | Server |
|---|---|
| `T/McpTests/McpToolPermissionModeChatTests.cs:111` (BUG 84298, TS claim) expects a saved allow-always to win in `ask` | `Lib` `types.d.ts:503`: "under ask never". The previous draft said "unless pre-approved"; corrected. The test is red by code reading and its comment says the expectation is the TS claim. |
| Previous draft: `0` as a preference `entityId` answers 400 | `T/PreferenceTests/PreferenceEntityIdTests.cs:146`: empty and `0` are the user's own scope; corrected. |
| Previous draft: a thread in a project answers 403 for an outsider of any role, 409 without a model; a Member's round runs on the project's model | Two project checks do not fire on thread create and a Read member's round is not bound (A16, A17); the spec now states only Room admin and User outsiders, the 409 on a round of a manager, and a manager's round. |
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

## Suspected defects

Re-checked 2026-11-06. No test was run; "red by code reading" means the assertion was compared with the code.

| # | Where | What the server does | Evidence | 2026-11-06 |
|---|-------|----------------------|----------|------------|
| A1 | Vectorization access | The old text (a manager of agent A queues a file of agent B) is narrower than thought: `GetVectorizationAvailabilityAsync` answers `AlreadyProcessed` before any right check, so an already processed file answers 200 to anyone, nothing queued, and shows the file exists (404 otherwise). A pending file is refused: the right needs Room manager. | `Security:681-691,2693-2706`; `VectorizationTaskPublisher.cs:68-76`; `TS/vectorization/vectorization.access.spec.ts:35` (now skipped) | STILL OPEN, narrowed; by code reading, no .NET test |
| A2 | Tool mode via preferences | Read was served from the ungated `/config` snapshot. | `Chat/storage/preferencesStorage.ts:51`; `T/PreferenceTests/PreferencePermissionTests.cs:198`, `PreferenceAiDisabledTests.cs:76` | FIXED (`4b380e5ee7`, `2a4975b142`) |
| A3 | Agent create | Room left behind when the binding failed. | route gone | FIXED by removal (`ecf76402b3`) |
| A4 | Default slot | Image-only Default handed to Chat. Default still takes it, resolve answers 404 and try-resolve null. | `T/AssignmentTests/AssignmentResolutionTests.cs:153,175`; `Chat/controllers/assignmentsController.ts:80-100,207-210` | FIXED (`d59923d4a0`) |
| A5 | Title regeneration | 500 on a real conversation. | `T/ThreadStorageTests/ThreadsRegenerateTitleTests.cs:102`; `T/ChatTests/ChatRegenerateTitleTests.cs:51` | FIXED (`876875d79d`, test comment), TS `test.fail` stale |
| A6 | Thread scope validation | File id, Trash accepted. | `T/ChatTests/ChatPermissionValidationTests.cs:58-64` | REFUTED: by design, reopen decision on bug 82719 |
| A7 | Web-search gates | Validation and probe before Guest and switch checks. | `Chat/controllers/webSearchController.ts:162-190`; `T/WebSearchTests/WebSearchConfigureTests.cs:154,170`, `WebSearchPermissionsTests.cs:181` | FIXED (`0dc046cacd`, `857671bb6d`) |
| A8 | MCP replace-all | Case twins stored. | `T/McpTests/McpServerReplaceAllTests.cs:227` (comment "used to") | FIXED (`b6d11f1999`, the library update) |
| A9 | Export drops the file | A form-filling room or a room out of quota answers 202 and writes nothing: the publisher checks the folder and the right only; the quota and the room rule meet the job later. | `AI/Core/MdTextToDocx/MdTextToDocxTaskQueue.cs:48-90`; `T/TextToDocxTests/TextToDocxQuotaTests.cs:51`, `TextToDocxRoomTypesTests.cs:73` | STILL OPEN, red by code reading |
| A10 | OpenAI passthrough bodies | 500 with a stack trace on a large body and malformed image requests. | commit message; `TS/openai-passthrough/openai-passthrough.spec.ts:404,497` keep old titles; no .NET test | FIXED (`9768f0a6f3`, `e84932692d`), by commit and code |
| A11 | Foreign prompt delete | `promptsStorage.delete` swallows the 404 and the controller answers success; the prompt survives. | `Chat/storage/promptsStorage.ts:148-157`; `Chat/controllers/promptsController.ts:295-304`; `T/PromptTests/PromptIsolationTests.cs:108` | STILL OPEN, red by code reading of those two files; the library engine between them was not read, and the test comment says "used to" |
| A12 | Server-executed tools | Foreign tenant answer: a client-supplied `Mcp-Session-Id` reached the shared container. | commit message | FIXED (`916074e486`) |
| A13 | Metadata gate | `assertAiAccess` borrows the gate through a throwaway tool-prefs read, now on four routes. | `Chat/controllers/aiAccess.ts:51`; `metaController.ts:148,161`; `profilesController.ts:253`; `editorToolsController.ts:130` | STILL OPEN |
| A14 | Contract drift | See the contract table. | yaml `5ea4dc6` | FIXED except the `aiTools` build field and its route (STILL OPEN) |
| A15 | Provider key leak | `aiProfilesList` hands the key to every member: Chat sends `forClient=true`, the storage controller reads no such flag and the service does not strip. | `Chat/storage/profilesStorage.ts:225,321`; `AI/Server/Api/ProfileStorageController.cs` (`ReadAllAsync`); `T/ProfileTests/ProfileAccessReadTests.cs:125` | NEW, red by code reading |
| A16 | Read member of a project | A round is not bound to the project: chat context drops a folder without `CanUseAi` (Room manager or Content creator), and the fallback reads `type` from the folder DTO, null for a room. The round runs on what the composer sent, without the project's model and sources. | `Security:2723-2735`; `AI/Server/Service/ChatContextService.cs:157-172`; `Chat/storage/docspaceFilesApi.ts:166,450-458`; `products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs:158,406-416`; `T/ChatTests/ChatProjectModelTests.cs:129` | NEW, red by code reading |
| A17 | Thread create in a project | Same null `type`: `aiThreadsCreate` never sees a project, so a DocSpace admin outside it creates a thread, and no model answers the generic 404, not 409. | `Chat/controllers/threadsController.ts:125-147`; `T/ChatTests/ChatProjectAccessTests.cs:71,111` | NEW, red by code reading |
| A18 | Web-search key | `aiWebSearchGetActiveConfig` returns the stored key to a User. | `Chat/controllers/webSearchController.ts:144-152`; `T/WebSearchTests/WebSearchStateTests.cs:194` | NEW, red by code reading |
| A19 | Resolve key | `aiAssignmentsResolveForAction` echoes the profile with its key. | `T/AssignmentTests/AssignmentPermissionTests.cs:116` (controller read only up to the resolve call) | NEW, per the test's comment, not fully traced |
| A20 | MCP credentials | The read routes redact only system servers, so a custom server's Authorization header reaches a User and a Room admin. | `Chat/controllers/toolsController.ts:126-130,227,239`; `T/McpTests/McpToolsAccessTests.cs:108` | NEW, red by code reading |
| A21 | Export names | `/` in a title is a path separator (`Notes 12/31` → `31.docx`); two simultaneous exports with one title get one name. | `T/TextToDocxTests/TextToDocxNamingTests.cs:118`, `TextToDocxConcurrencyTests.cs:68` (bugs 82711, 84459, "measured" in the comments) | NEW, per the tests, code not read |
| A22 | Export of formulas; failure language | TeX source lands in the document; a stored provider failure stays English under any culture. | `T/MessageStorageTests/MessageMarkdownDocxTests.cs:99` (83038); `T/ChatTests/ChatFailureLanguageTests.cs:81` (83046) | NEW, per the tests, code not read |
| A23 | `aiThreadsOpenOrCreate` without a message | No `threadId`, no `firstMessage`: 500 from the engine. | `T/ThreadStorageTests/ThreadsOpenOrCreateTests.cs:156` (82826) | NEW, per the test comment |
| A24 | Concurrent add of one name | Losers answer 403, not the soft "already registered". | `T/McpTests/McpServerConcurrencyTests.cs:62` | NEW, per the test comment |
| A25 | Profile `createdAt` | The create answer has milliseconds no later read reproduces. | `T/ProfileStorageTests/ProfileReadTests.cs:47` | NEW, per the test comment |
| A26 | Metadata routes | A failed model call relays the provider's text for every failure, while `aiSkillsGenerate` was narrowed to a rate limit, a lack of credit and the portal's spending limit because an authentication failure quotes the masked API key. | `common/ASC.AI.Chat/app/controllers/metaController.ts:137` against `skillsController.ts:72-83`; `72adbfaa14` | NEW, red by code reading; whether a provider's auth text reaches a caller without provider rights not traced |
