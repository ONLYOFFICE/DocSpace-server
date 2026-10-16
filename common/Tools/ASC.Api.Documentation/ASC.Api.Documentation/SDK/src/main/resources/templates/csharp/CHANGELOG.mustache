# Change Log

## 4.0.0

### Added

- Added the reworked AI API surface in `src/DocSpace.API.SDK/Api/AI/`: `AIApi`, `AssignmentsApi`, `AttachmentsApi`, `EditorToolsApi`, `ExportApi`, `OpenAIPassthroughApi`, `PreferencesApi`, `ProfilesApi`, `PromptsApi`, `ThreadsApi`, `ToolsApi`, `WebSearchApi`
- Added per-user AI settings endpoints and the matching models `AiUserSettingsDto`, `AiUserSettingsWrapper`, `AiSettingsDto`, `AiSettingsWrapper`
- Added the `AppsApi` (`Api/Apps/`), `PrivacyRoomApi` (`Api/Rooms/`) and `DocsCloudApi` (`Api/Settings/`) APIs

### Changed

- Renamed the AI operations after their tag: `CreateAgent` → `AiAgentsCreate`, `GetAiSettings` → `AiSettingsGet`, `GetAiUserSettings` → `AiSettingsGetUser`, `SetAiUserSettings` → `AiSettingsSetUser`
- The AI operations now use their own `Ai`-prefixed models (for example `AiFolderIntegerWrapper` instead of `FolderIntegerWrapper`), so they no longer share the models of the Files and Portal APIs
- Renamed the models whose names did not say what they carry; the JSON sent and received is unchanged unless stated: a JSON body nested in a request takes a `Request` suffix (`Delete` → `DeleteFileRequest`, `CreateFolder` → `CreateFolderRequest`, `CreateFileJsonElement` → `CreateFileRequest`, `MentionMessageWrapper` → `MentionMessageRequest`), a body sent on its own a `RequestDto` suffix (`CspRequestsDto` → `CspRequestDto`), returned objects a `Dto` suffix (`AceShortWrapper` → `AceShortDto`, `TimezonesRequestsDto` → `TimezoneDto`, `IAccountEntryDto` → `AccountEntryDto`), and generic or internal names became specific (`Status` → `ExternalShareStatus`, `Options` → `DocumentOptionsDto`, `SsoSettingsV2` → `SsoSettingsDto`, `STRINGArrayWrapper` → `StringArrayWrapper`, `EncryprtionStatus` → `EncryptionStatus`, `AiTProvider` → `AiProvider`)
- Settings, database entities and other server-side classes are no longer published as models: each endpoint that returned or accepted one now uses a dedicated model of the same shape (`TenantWalletSettings` → `TenantWalletSettingsDto`, `FireBaseUser` → `FirebaseDeviceDto`, `UserInfo` → `PortalUserDto`, also as the `user` of `MentionDto`, `TenantQuota` → `TenantQuotaDto`, `Balance` → `BalanceDto`, `DocsCloudConfig` → `DocsCloudConfigDto`, `BackupProgress` → `BackupProgressDto`, `CoEditingConfig` → `CoEditingConfigDto`, `Logo` → `LogoDto`, `ChunkedUploadSessionResponse` → `ChunkedUploadSessionDto`, `ThirdPartyParams` → `ThirdPartyAccountDto`, `CompanyWhiteLabelSettings` → `LicensorDetailsDto` and `AdditionalWhiteLabelSettings` → `AdditionalResourcesDto` in the answers of the rebranding resets); the settings nested in a request body take the name of their operation (`TenantWalletSettings` → `SetWalletTopUpSettingsRequest`, `TenantAuditSettings` → `SetAuditLifetimeSettingsRequest`, `CompanyWhiteLabelSettings` → `SaveCompanyInfoRequest`, `AdditionalWhiteLabelSettings` → `SaveAdditionalResourcesRequest`) and keep every field, including those the server sets itself and does not read (`lastModified`, `lowBalanceNotified`); `POST /api/2.0/settings/authservice` takes its own `SaveAuthKeysRequestDto` of the same shape instead of the `AuthServiceDto` it returns
- The third-party accounts returned by `GET /api/2.0/files/thirdparty` no longer have the `auth_data` property, and the devices returned by `POST /api/2.0/settings/push/docregisterdevice` and `PUT /api/2.0/settings/push/docsubscribe` no longer have `tenant`; neither was ever filled in
- Renamed the AI chat operations that repeated the `ai` prefix: `aiAiSend` → `aiSend`, `aiAiSendCustom` → `aiSendCustom`, `aiAiSendWithStream` → `aiSendWithStream`, `aiAiSendWithStreamOpenAI` → `aiSendWithStreamOpenAI`, `aiAiRegenerateStream` → `aiRegenerateStream`, `aiAiApproveToolCall` → `aiApproveToolCall`, `aiAiDenyToolCall` → `aiDenyToolCall`
- `checkMoveOrCopyBatchItems` (`GET /api/2.0/files/fileops/move`) and `checkMoveOrCopyDestFolder` (`GET /api/2.0/files/fileops/checkdestfolder`) take the batch as separate query parameters (`folderIds`, `fileIds`, `destFolderId`, `conflictResolveType`, `deleteAfter`, `content`, `toFillOut`, `returnSingleOperation`) instead of one `inDto` object, which is how the server reads them
- Renamed the activity-log models after what they describe (`HistoryData` → `HistoryDataDto`, `HistoryAction` → `HistoryActionDto`, `EntryData` → `EntryHistoryDataDto`, `FileData` → `FileHistoryDataDto`, `LinkData` → `LinkHistoryDataDto`, `TagData` → `TagHistoryDataDto`) and the backup request models after their operation (`BackupDto` → `StartBackupRequestDto`, `BackupRestoreDto` → `StartBackupRestoreRequestDto`, `BackupScheduleDto` → `CreateBackupScheduleRequestDto`, `Cron` → `BackupCronRequest`); the JSON is unchanged

### Removed

- Removed the superseded AI API classes `ChatApi`, `MCPApi`, `MessagesApi` and `ProvidersApi` from `src/DocSpace.API.SDK/Api/AI/`
- Removed the superseded AI settings model `SetAiUserSettingsRequestDto`; `AiSettingsDto`, `AiSettingsWrapper`, `AiUserSettingsDto` and `AiUserSettingsWrapper` keep their names but now describe the reworked AI settings

## 3.7.0

### Added

- Added new API methods and enhanced models with additional properties
- Added tag Rooms / Groups
- Added per-user AI settings endpoints: `GetAiUserSettings` (GET `/api/2.0/ai/config/user`) and `SetAiUserSettings` (PUT `/api/2.0/ai/config/user`)
- Added new models `AiUserSettingsDto`, `AiUserSettingsWrapper`, and `SetAiUserSettingsRequestDto` (recommended model banner visibility)
- Added `UserUpdatedAiSettings` value to the `MessageAction` enum
- Added a Rate Limiting section to the README describing the `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`, and `Retry-After` response headers
- Added a dedicated NuGet package readme (`README_nuget.md`) via `PackageReadmeFile`
- Added XML documentation to `ApiDateTimeConverter` (`ReadJson`/`WriteJson`)

### Changed

- Updated from System.Text to Newtonsoft
- Updated SDK OpenAPI specification v3.7.0
- Updated example values, added email length validation, and adjusted method return types in API models and methods
- Date range parameters (`from`/`to` UTC time) are now always sent for audit trail, login history, and file/folder index requests

### Fixed

- Fixed & / ' issues
- Fixed ApiDateTimeConverter
- Fixed descriptions
- Fixed the `system` parameter reference in the Web plugins API documentation
- Fixed HTML encoding of special characters (`'`, `&`, `<>`) in generated documentation comments

### Improved / Enhanced

- Enhanced API models with detailed parameter descriptions, updated example values, and added validation for required fields
- .NET 10 update

## 3.6.0

- Fixed enum formatting and corrected data types in generated models
- Updated method descriptions and added missing/new fields
- Regenerated SDK based on OpenAPI specification v3.6.0

## 3.5.0

- Initial release
