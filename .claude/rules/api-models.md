---
paths:
  - "**/*Controller.cs"
  - "**/ApiModels/**/*.cs"
  - "**/Models/RequestDto/**/*.cs"
  - "**/Models/ResponseDto/**/*.cs"
  - "common/ASC.Api.Core/Model/**/*.cs"
---

# API models: what may cross the HTTP boundary

The REST contract is published as OpenAPI, turned into eight SDKs and into agent tool definitions. A class
that appears in it is a promise to every one of those consumers. A class of the core — a setting, an EF
entity, a service — has other owners: storage, caching, other services. When one class plays both roles,
a change made for storage silently changes the API, and a field meant for the server becomes one a client
can read or set.

This file covers which types an endpoint may take and return. How their texts read is
`.claude/rules/openapi-dto-docs.md` (properties) and `.claude/rules/openapi-endpoint-docs.md` (operations).

## 1. The boundary

Everything a controller action sends or receives is an **API model**: a class in the `ApiModels`
(`RequestDto`/`ResponseDto`) folder of the service, or a shared API model in `common/ASC.Api.Core/Model`
(`EmployeeDto`, `GroupDto`, `ApiDateTime`). That covers:

- the return type of the action (after `Task<>`, `ActionResult<>`, `IAsyncEnumerable<>`, lists);
- the `[FromBody]`, `[FromQuery]` and `[FromRoute]` models;
- **every property of those classes, at any depth.** A DTO whose property is a core class leaks that
  class just the same (`MentionWrapper.User` was a full `UserInfo`, `SettingsDto` carries `PasswordHasher`).

Allowed as leaves without a DTO: primitives, `string`, `Guid`, `DateTime`/`DateTimeOffset`, `ApiDateTime`,
enums (they travel as numbers), collections of allowed types, and file results (`IActionResult`,
`FileResult`, `Stream`).

Never on the boundary:

- `ISettings` classes — stored as JSON in the settings table, so their shape is a storage format;
- EF entities (`Db*`, `FireBaseUser`) and their navigation properties;
- core domain classes (`UserInfo`, `Tenant`, `TenantQuota`, `File<T>`, `Folder<T>`);
- contracts of other systems — the Document Server editor config (`Configuration.cs`: `Options`,
  `WatermarkOnDraw`, …), billing client models (`DocsCloud*`, `Accounting*`);
- services and interfaces (`PasswordHasher`, `ICompress`, `TenantDomainValidator`);
- internal service wrappers (`WCFService/Wrappers/*Wrapper`).

## 2. Request and response are separate classes

A request DTO carries only what the caller is allowed to choose. Anything the server owns — `lastModified`,
state flags, counters, ids it assigns — stays out of it, so there is nothing to strip or "carry over" in the
controller. Reusing the response class as the body, or the stored class as either, is what let
`TenantWalletSettings.lowBalanceNotified` and a client-supplied `lastModified` reach `SaveAsync`.

The controller builds the domain object from the request explicitly and fills the server-owned fields itself
(`PaymentsController.SetTenantWalletSettings`, `SecurityController.SetAuditSettings`).

## 3. Mapping

First decide whether a DTO is a copy at all. A class that exists only to be returned — nothing but the
code that fills the response reads it — is already an API model in the wrong place: rename it to `<Entity>Dto`
and move it into `ApiModels`, with no copy and no mapper (the editor-config classes of `Configuration.cs` →
`Files/Core/ApiModels/ResponseDto/EditorConfigDto.cs`). Move it into the `ApiModels` of the assembly that
builds it; when that assembly has none (Web.Core, Backup.Core), move the building code up to the API project
with it (`AuthService` → `AuthServiceDto.From`, `ExternalResourceSettings` → `ExternalResourcesDtoHelper`,
`BackupService.GetScheduleAsync` now returns `ScheduleResponse` and the controller builds `ScheduleDto`).

A copy is right only when the class lives on without the API: a setting, an entity, a domain class read by
other code, a service result, or the contract of an external service (billing, DocsCloud, the AI gateway).

- **1:1 copy → Mapperly**, with `RequiredMappingStrategy.Target` towards a DTO: every DTO property must be
  mapped, and a property added to the domain class does not appear in the API by itself. A static extension
  mapper next to the DTO is enough (`TenantWalletSettingsDtoMapper`, `FirebaseDeviceDtoMapper`). A request DTO
  mapped onto a domain or settings class uses `RequiredMappingStrategy.Source` instead, so a request field
  nothing reads breaks the build (`IpRestrictionEntryDtoMapper`); details in `.claude/rules/dto-mapping.md`.
- **Access checks, async lookups, computed fields → a `[Scope]` `*DtoHelper`** (`FileDtoHelper`,
  `EmployeeDtoHelper`). A person in a new endpoint is an `EmployeeDto`/`EmployeeFullDto` from
  `EmployeeDtoHelper`, never a `UserInfo`. `PortalUserDto` — the stored user record field for field — exists
  only to keep the JSON of old endpoints that always returned it (`GET portal/users/{userId}`, the `user` of
  `MentionDto`); do not use it anywhere new.
- The service layer may keep returning its own types; the controller maps at the boundary
  (`EditorController.GetSharedUsers`: `MentionWrapper` → `MentionDto`).

## 4. Names and markup

- The class name is the schema name (§3.6 of `openapi-dto-docs.md`), so it follows one convention:
  - the model an action binds — the `[FromRoute]`/`[FromQuery]`/`[FromBody]` wrapper or a plain body
    parameter — is `<Action><Entity>RequestDto` (`CreateFolderRequestDto<T>`, `StartEditRequestDto<T>`);
  - the JSON body nested in such a wrapper is `<Action><Entity>Request` (`CreateFolderRequest`,
    `StartEditRequest`, `MentionMessageRequest`) — the two cannot share a name, and the body is what SDK
    users and agents see;
  - a returned object is `<Entity>Dto`; never `Requests`, `Body`, `Parameters`, `Wrapper` or a bare noun
    (`Delete`, `Culture`).
- `<summary>`, `<example>`, validation attributes (`[Range]`, `[StringLength]`) and `[JsonPropertyName]`
  for the API belong on the DTO, not on a core class.
- `[OpenApiSchemaName]` is for what cannot carry a good contract name itself: an enum of the core whose type
  name is too generic for the contract (`Area` → `AccountSearchArea`, `Status` → `ExternalShareStatus`), an
  interface (`IAccountEntryDto` → `AccountEntryDto`, the `I` is C# convention), and a generic type. An API
  model gets the right class name instead, and a core class gets a DTO (§1).

## 5. Moving an existing endpoint to a DTO

Most exposed core classes predate this rule. Moving one is a contract change only if you make it one:

1. The new DTO reproduces today's JSON **field for field**, including quirks (`[JsonPropertyName("IsLicensor")]`,
   snake_case of an external contract) — clients see no difference. Keep the response schema name where you
   can, so the SDK model keeps its name too.
2. Dropping a leaked or server-owned field is a separate, deliberate step. Before it, check the web client
   (`../client`, `packages/shared/api`) and say in the PR which consumers you could not check (mobile apps).
3. Verify with a diff of the OpenAPI documents before and after (stage A of the `generate-sdk` skill): only
   the schemas you meant to change may differ. Run the integration tests of the endpoints.

## Definition of done

- [ ] No type on the endpoint's request or response graph is a setting, an entity, a core or foreign class,
      a service or an internal wrapper (§1).
- [ ] The request DTO holds only caller-chosen fields; the controller sets the server-owned ones (§2).
- [ ] Mapping is Mapperly (`Target` towards a DTO, `Source` from a request) or a `*DtoHelper`; people in new endpoints are `EmployeeDto` (§3).
- [ ] When an endpoint was moved: the OpenAPI diff shows only the intended changes, and any removed field is
      named in the PR together with the consumers checked (§5).
