---
paths:
  - "**/*.cs"
---

# Converting to DTOs: a Mapperly mapper, not a helper

The mapping library is **Riok.Mapperly** (referenced from `ASC.Common`, so every project has it).
Mapster and AutoMapper were removed — never bring them, or any second mapping library, back.

## Checklist

- Every new conversion is a Mapperly mapper: no new `*Helper` method, inline initializer or other library (Rule)
- Server-owned target fields (`LastModified`, `IsLicensor`) are set explicitly in the controller (Rule)
- A source class that exists only for the response is moved into `ApiModels` instead of mapped (Rule)
- The mapper follows its target type in the same file, named `<Type>Mapper`/`<Type>DtoMapper` (How to declare)
- Strategy `Target` to an API DTO, `Source` from a request; `None` only where already used (How to declare)
- Renames use `[MapProperty(nameof(...), nameof(...))]`; DI instances only via `[ObjectFactory]` (How to declare)
- No I/O or `.Result` in a generated mapping; enriched fields come from a `*DtoHelper` (When a helper is still right)
- A touched helper method that is only a flat copy is replaced by a mapper in the same change (Existing helpers)

## Rule

Any **new** conversion — entity → DTO, request DTO → settings/domain, EF row ↔ domain — is a
Mapperly mapper. Not a new `*Helper` / `*DtoHelper` method, and not an object initializer
assembled inline in a controller or service.

One exception: when the server fills fields of the target itself (`LastModified`, a low-balance flag,
`IsLicensor`), the controller builds that object explicitly, so the server-owned fields are visible
where they are set (`.claude/rules/api-models.md` §2; `PaymentsController.SetTenantWalletSettings`).

Before writing a mapper at all, check whether the source class exists only for the response — then
it is renamed and moved into `ApiModels` instead of copied (`api-models.md` §3).

## How to declare one (follow the existing pattern)

- Same file as the target DTO or entity, right after the type; name it `<Type>Mapper` or
  `<Type>DtoMapper`.
- The required-mapping strategy depends on the direction, because it decides which mistake the
  build catches:

  | Direction | Attribute | What breaks the build |
  |---|---|---|
  | domain/settings/entity → **API DTO** | `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]` | a DTO property nothing fills |
  | **request DTO** → domain/settings | `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]` | a request field nothing reads |
  | EF row ↔ domain, existing mappers | `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]` | nothing — keep it only where the repo already uses it |

  `Target` on a request mapping would force mapping the server-owned fields of the target, which
  the request must not carry; `Source` checks the request side instead and leaves them alone.
- **Pure copy, no services** — a static extension mapper, called as `x.Map()`:

  ```csharp
  [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
  public static partial class TenantWalletSettingsDtoMapper
  {
      public static partial TenantWalletSettingsDto Map(this TenantWalletSettings source);
  }
  ```

- **Needs services** (`ApiDateTimeHelper`, `TenantUtil`, `UserManager`, `EmployeeDtoHelper`...) — a
  `[Scope]` partial class with a primary constructor, injected like any service. A private partial
  `Map` does the flat copy with the enriched targets marked `[MapperIgnoreTarget(nameof(...))]`; a
  public `MapToDto` (`[UserMapping(Default = true)]`) or `async Task<XDto>` method fills them in.
  Reference: `EditHistoryMapper` (`products/ASC.Files/Server/ApiModels/ResponseDto/EditHistoryDto.cs`).
- Renames with `[MapProperty(nameof(...), nameof(...))]` — `nameof`, never string literals; type
  conversions as private methods (`Use = nameof(...)`); instances from DI only through
  `[ObjectFactory]` (see `FileMapper` in `Core/Entries/File.cs`).

## When a helper is still right

Building the DTO needs per-entry security (`FileSecurity`), DAO lookups, link or URL building,
per-request caching, or user resolution with fallbacks — what `FileDtoHelper`, `FolderDtoHelper`,
`FileEntryDtoHelper` and `EmployeeDtoHelper` do. Do not cram that into a mapper: let the mapper do the
flat copy and call the helper for the enriched fields (as `ApiKeyMapper` calls `EmployeeDtoHelper`).
Never hide I/O or `.Result` inside a generated mapping path.

## Existing helpers

When you touch a helper method that is only a flat property copy, replace it with a mapper in the
same change (precedent: `a575fed6b4`, `EditHistoryMapper` out of `FilesControllerHelper`). Do not
rewrite the big enrichment helpers as a side effect of an unrelated change.
