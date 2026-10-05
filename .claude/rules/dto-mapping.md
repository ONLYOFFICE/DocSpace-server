---
paths:
  - "**/*.cs"
---

# Converting to DTOs: a Mapperly mapper, not a helper

The mapping library is **Riok.Mapperly** (referenced from `ASC.Common`, so every project has it).
Mapster and AutoMapper were removed — never bring them, or any second mapping library, back.

## Rule

Any **new** conversion — entity → DTO, request DTO → settings/domain, EF row ↔ domain — is a
Mapperly mapper. Not a new `*Helper` / `*DtoHelper` method, and not an object initializer
assembled inline in a controller or service.

## How to declare one (follow the existing pattern)

- Same file as the target DTO or entity, right after the type; name it `<Type>Mapper` or
  `<Type>DtoMapper`.
- Attribute, exactly as the rest of the repo writes it:
  `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]`.
- **Pure copy, no services** — a static extension mapper, called as `x.MapToDto()`:

  ```csharp
  [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
  public static partial class TenantDtoMapper
  {
      [MapProperty(nameof(Tenant.Id), nameof(TenantDto.TenantId))]
      public static partial TenantDto MapToDto(this Tenant source);
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
