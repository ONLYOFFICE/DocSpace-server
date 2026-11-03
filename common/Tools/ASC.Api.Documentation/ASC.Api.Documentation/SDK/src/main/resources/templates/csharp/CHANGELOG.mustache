# Change Log

## 4.1.0

### Added

- Added the AI project API in `src/DocSpace.API.SDK/Api/Rooms/AIApi.cs` (`DocSpace.API.SDK.Api.Rooms.AIApi`): `GetProjectModel` and `SetProjectModel` (the chat model of an AI project), `GetProjectSources`, `LinkProjectSources` and `UnlinkProjectSources` (the rooms linked to a project as its sources), `GetRoomSkills` and `GetRoomsSkills` (the skills of an Instructions room and the skills catalog), with the models `AiProjectModel`, `AiProjectModelDto`, `AiProjectModelWrapper`, `AiProjectSourceDto`, `AiProjectSourcesDto`, `AiProjectSourcesWrapper`, `AiSkillDto`, `AiSkillsDto`, `AiSkillsRoomDto`, `AiSkillsWrapper`, `LinkAiProjectSourcesRequest`, `UnlinkAiProjectSourcesRequest` and `UpdateAiProjectModelRequest`
- Added `AiContextGetKnowledgeFolders` to the AI `ContextApi`
- `AiAssignmentsGetAllAssignments` takes `expand`: `profile` answers the assigned profiles themselves instead of their IDs
- Added `hasMetadataFields` to `EntryMetadataDto`
- Added enum values: `RoomType.InstructionsRoom`, `KnowledgeRoom` and `AiProjectRoom`; `FolderType.InstructionsRoom`, `KnowledgeRoom`, `AiProjectRoom` and `AiHub`; `FilterType.InstructionsRooms`, `KnowledgeRooms` and `AiProjectRooms`; `SearchArea.AiHub`; `SubjectType.AiProject`; `MessageAction.RoomsLinkedToAiProject`, `RoomsUnlinkedFromAiProject` and `AiProjectProfileAssigned`
- The chat-service answers that were declared as a bare `{ success }` are typed: `attachments/suggested-questions` returns `AiSuggestedQuestionsResult` (`status` and `questions` of `AiSuggestedQuestion`), `context/get-context-folders` and `context/get-knowledge-folders` a list of `AiContextFolder`, `context/get-room-skills` a list of `AiRoomSkill`, `context/get-room-skill` the skill text, and `config/tool-mode` (read and write) `AiToolPermissionModeWrapper`
- Request fields the chat service already read: `AiActionArgs` gains `contextRooms` (`AiContextRoomRef`), `knowledgeRooms` (`AiKnowledgeRoomRef`), `folderContext` (`AiFolderContextRef`), `toolInstructions` and `entityId` (`knowledgeRoom` is deprecated); `contextEntityId` is added to `AiSendRequest`, `AiSendStreamBody`, `AiRegenerateStreamRequest` and `AiToolCallData`, `profileId` to `AiSendCustomRequest`, `messageId`, `keepCount` and `expectedLength` to `AiRegenerateStreamRequest`, and `format` (`Docx`, `Pdf` or `Md`) to `AiExportTextToDocxRequest`
- `AiThreadMessageLike` describes what is stored: `status` carries `reason` and `error` (`AiChatErrorPayload`), and the content parts carry the tool-call fields (`toolName`, `toolCallId`, `args`, `result`) and the file fields (`mimeType`, `data`, `image`, `name`, `filename`)

### Changed

- **Breaking:** `threads/append-user-message` answers `messageId` as the stored message (`AiThreadMessageLike`, its ID in `messageId.id`) - the service always sent it so, the SDK declared a string
- **Breaking:** `assignments/get-all-assignments` answers one of two shapes: the map of action to profile ID, or with `expand=profile` `AiAssignmentsExpanded` (`assignments`, `profilesTotal`, `chatProfilesTotal`)
- **Breaking:** `attachments/suggested-questions` takes the attachment ID itself as the body (a string) instead of an object
- `AiPromptBundle.version` is an integer, so `prompts/import-bundle` accepts a bundle exported through the SDK (the version used to be sent as the string "1" and the import refused it)
- `ApiClient`: an operation that returns `bool` or an enum throws `ApiException` with the status on a non-2xx answer instead of `NullReferenceException`; a 2xx answer that carries no value (no body or a JSON `null`) throws `ApiException` with that status instead of `NullReferenceException` or `ApiException(500)`; and an operation whose answer is a `oneOf`/`anyOf` model (for example `AiAssignmentsGetAllAssignments`, `AiProfilesTestConnection`) deserializes it instead of throwing `ArgumentException`
- The inline models of `FileEntryDto` lost their `Ai` prefix: `AiFileEntryDtoAllOfAvailableShareRights`, `AiFileEntryDtoAllOfSecurity` and `AiFileEntryDtoAllOfShareSettings` are now `FileEntryDtoAllOfAvailableShareRights`, `FileEntryDtoAllOfSecurity` and `FileEntryDtoAllOfShareSettings`
- The API version is 4.1.0

### Removed

- Removed the AI agents: `AgentsApi` in `Api/AI/` (`AiAgentsList`, `AiAgentsCreate`, `AiAgentsGet`, `AiAgentsUpdate`, `AiAgentsDelete`, `AiAgentsNews`, `AiAgentsUpdateQuota` and `AiAgentsResetQuota`) and the `Ai`-prefixed models it used (`AiFolderWrapper`, `AiFolderContentWrapper`, `AiFileOperationWrapper`, `AiAgentsCreateRequest`, `AiAgentsUpdateRequest` and the others). The server no longer serves `/api/2.0/ai/agents`; an AI project (`RoomType.AiProjectRoom`) takes the place of an agent

## 4.0.0

### Added

- Third-party storage support: 100 operations that address a file, folder or room (for example `GetFileInfo`, `GetFolderByFolderId`, `UpdateRoom`, `UploadFile`, the sharing and upload-session operations) have an overload that takes the string identifier of an entry in a connected third-party storage (for example `"sbox-42"`) and returns the matching `ThirdParty*` model (`ThirdPartyFileWrapper`, `ThirdPartyFolderWrapper`, `ThirdPartyFolderContentWrapper`, `ThirdPartyChunkedUploadSessionWrapper` and others)
- Added the reworked AI API in `src/DocSpace.API.SDK/Api/AI/`: `AIApi` (sending messages with or without streaming, regenerating answers, approving and denying tool calls), `AssignmentsApi`, `AttachmentsApi`, `ContextApi`, `EditorToolsApi`, `ExportApi`, `OpenAIPassthroughApi`, `PreferencesApi`, `ProfilesApi`, `PromptsApi`, `ThreadsApi`, `ToolsApi` and `WebSearchApi`; the AI `SettingsApi` gains `AiSettingsGetToolMode` and `AiSettingsSetToolMode`
- Added `MetadataApi` (`Api/Files/`): metadata templates and their fields, assigning templates to files and folders, custom fields and field values; the folder listings (`GetFolderByFolderId`, `GetMyFolder`, `GetRecentFolder`, `GetFavoritesFolder`, `GetTrashFolder`, `GetRoomsFolder`) take `metadataTemplateId` and `metadataFilters`, and the new `SearchFolder` and `SearchRooms` find entries by their metadata
- Added `AppsApi` (`Api/Apps/`), `PrivacyRoomApi` (`Api/Rooms/`, the users' encryption keys for privacy rooms) and `DocsCloudApi` (`Api/Settings/`)
- Added the Files operations `GetFormsFolder`, `GetRoomAiFolder`, `ShowQuickActions`, `GetEncryptionInfo`, `SetEncryptionInfo` and `GetEncryptionAccess`; `GetRoomsFolder` takes `privacyFilter` and `withAiFolder`, `GetFolderByFolderId` and `EmptyTrash` take `folderType`, `GetRoomGroups` takes `searchArea`
- Report generation: `CreateReportFolderHistory`, `CreateAuditTrailReport` and `CreateLoginHistoryReport` take `format`, `from` and `to` and return a `DocumentBuilderTaskWrapper`; the new `GetReportFolderHistory`, `GetAuditTrailReport`, `GetLoginHistoryReport` and the matching `Terminate*` operations follow and cancel the report
- Added the payment operations `GetActiveServices`, `GetAccountingServicePrices`, `GetSubscriptionBalanceInfo`, `MoveSubscriptionToWallet`, `GetCustomerServiceUsage` and `GetCustomerMonthlyUsage` with their `Create*Report`, `Get*Report` and `Terminate*Report` operations, and `GetUpcomingPayments` in `QuotaApi`; `GetPaymentQuotas` takes `additional`
- Added `GetBackupsCounts` (`BackupApi`) and `RefreshWebPlugin` (`WebpluginsApi`)
- Added model properties: `showQuickActions` to `FilesSettingsDto`, `formsUsedSpace` to `FilesStatisticsResultDto`, `ai` to `CustomizationConfigDto`, `isDelayedPaymentMethod` and `paymentMethodType` to `CustomerInfoDto`, `searchArea` to `RoomGroupDto` and `RoomGroupRequestDto`, `walletLowBalance` to `SettingsDto`, `runtime` to `WebPluginDto`, `toolPermissionMode` to `AiSettingsDto` and `AiUserSettingsDto`, `lastModified` to `TenantAiAccessSettingsDto`, `TenantBannerSettingsDto` and `TenantDevToolsAccessSettingsDto`, `errors` to `ProblemDetail`; `OperationDto` describes the source of a charge with `sourceId`, `sourceTitle`, `sourceType`, `cost` and `tokenUsage`
- Added enum values: `FolderType.Forms` and `FolderType.Ai`; `TenantWalletService.Admin`, `DocsCloud`, `DocsCloudDevPack` and `AISearch`; new audit actions in `MessageAction`
- Added the error body models `ErrorApiResponse` and `ValidationErrorResponse`

### Changed

- Files, folders and rooms use one set of models for numeric identifiers: the `Integer` suffix is gone (`FileIntegerWrapper` → `FileWrapper`, `FolderIntegerWrapper` → `FolderWrapper`, `FolderContentIntegerWrapper` → `FolderContentWrapper`, `FileDtoInteger` → `FileDto`, `FolderDtoInteger` → `FolderDto`, `ConfigurationIntegerWrapper` → `ConfigurationWrapper`, `UploadSessionResponseIntegerWrapper` → `UploadSessionResponseWrapper`, `OrdersRequestDtoInteger` → `OrdersRequestDto`, `UpdateRoomsQuotaRequestDtoInteger` → `UpdateRoomsQuotaRequestDto` and so on), and the `String` variants became `ThirdParty*` models (`FolderStringWrapper` → `ThirdPartyFolderWrapper`, `FolderDtoString` → `ThirdPartyFolderDto`, `FileEntryDtoString` → `ThirdPartyFileEntryDto`); the chunked-upload operations return `ChunkedUploadSessionWrapper` and `ChunkedUploadSessionResultWrapper`
- Renamed the request models with the plural `Requests` to the singular (`AuthRequestsDto` → `AuthRequestDto`, `SsoSettingsRequestsDto` → `SsoSettingsRequestDto`, `TfaRequestsDto` → `TfaRequestDto`, `StorageRequestsDto` → `StorageRequestDto`, `CspRequestsDto` → `CspRequestDto`, `WhiteLabelRequestsDto` → `WhiteLabelRequestDto`, `WebPluginRequests` → `WebPluginRequest` and the rest), and the JSON bodies nested in a request take a `Request` suffix (`Delete` → `DeleteFileRequest`, `DeleteFolder` → `DeleteFolderRequest`, `CreateFolder` → `CreateFolderRequest`, `CreateFileJsonElement` → `CreateFileRequest`, `CreateTextOrHtmlFile` → `CreateTextOrHtmlFileRequest`, `CopyAsJsonElement` → `CopyAsRequest`, `UpdateFile` → `UpdateFileRequest`, `LockFileParameters` → `LockFileRequest`, `StartEdit` → `StartEditRequest`, `MentionMessageWrapper` → `MentionMessageRequest`, `EmailValidationKeyModel` → `CheckConfirmRequestDto`); the JSON sent is unchanged
- Settings, database entities and other server-side classes are no longer published as models: each one became a `Dto` of the same shape (`TenantQuota` → `TenantQuotaDto`, `Balance` → `BalanceDto`, `Tariff` → `TariffDto`, `BackupProgress` → `BackupProgressDto`, `CoEditingConfig` → `CoEditingConfigDto`, `Logo` → `LogoDto`, `StorageSettings` → `StorageSettingsDto`, `DarkThemeSettings` → `DarkThemeSettingsDto`, `WizardSettings` → `WizardSettingsDto` and so on) or a model named after what it carries (`UserInfo` → `PortalUserDto`, `FireBaseUser` → `FirebaseDeviceDto`, `ThirdPartyParams` → `ThirdPartyAccountDto`, `CompanyWhiteLabelSettings` → `LicensorDetailsDto`, `AdditionalWhiteLabelSettings` → `AdditionalResourcesDto`, `SsoSettingsV2` → `SsoSettingsDto`, `IPRestriction` → `IpRestrictionDto`, `SetupCode` → `TfaSetupCodeDto`); the room, user and AI agent quota settings are all returned as `EntityQuotaSettingsWrapper`
- The settings sent in a request body take the name of their operation and keep every field (`TenantWalletSettingsWrapper` → `TenantWalletSettingsRequestDto` with `SetWalletTopUpSettingsRequest`, `TenantAuditSettingsWrapper` → `TenantAuditSettingsRequestDto` with `SetAuditLifetimeSettingsRequest`, `CompanyWhiteLabelSettingsWrapper` → `CompanyWhiteLabelSettingsRequestDto` with `SaveCompanyInfoRequest`, `AdditionalWhiteLabelSettingsWrapper` → `AdditionalWhiteLabelSettingsRequestDto` with `SaveAdditionalResourcesRequest`, `ExternalDatabaseSettings` → `ExternalDatabaseConnectionRequestDto`, `EmailActivationSettings` → `EmailActivationSettingsRequestDto`); `SaveAuthKeys` takes `SaveAuthKeysRequestDto` and `GetAuthServices` returns `AuthServiceArrayWrapper`
- Renamed other models whose names did not say what they carry: `Status` → `ExternalShareStatus`, `Options` → `DocumentOptionsDto`, `STRINGArrayWrapper` → `StringArrayWrapper`, `EncryprtionStatus` → `EncryptionStatus`, `AceShortWrapper` → `AceShortDto`, `MentionWrapper` → `MentionDto`, `TimezonesRequestsDto` → `TimezoneDto`, `GroupMemberSecurityRequestDto` → `GroupMemberSecurityDto`, `SmtpOperationStatusRequestsDto` → `SmtpOperationStatusDto`, `KeyValuePairBooleanString` → `ItemKeyValuePairBooleanString`, `HistoryData` → `HistoryDataDto`, `HistoryAction` → `HistoryActionDto`, `Area` → `AccountSearchArea`, `Location` → `RequestLocation`, `TfaRequestsDtoType` → `TfaType`, and the backup request models after their operation (`BackupDto` → `StartBackupRequestDto`, `BackupRestoreDto` → `StartBackupRestoreRequestDto`, `BackupScheduleDto` → `CreateBackupScheduleRequestDto`, `Cron` → `BackupCronRequest`)
- Operations that returned an untyped `ObjectWrapper` or `ObjectArrayWrapper` now return a typed model: `StringWrapper` (`GetPortalPath`, `GetPortalHostname`, `GetGreetingSettings`, `GetThirdPartyCode`), `StringArrayWrapper` (`GetRoomTagsInfo`), `TokenDiagnosticsWrapper` (`GetClaims`), `AuditTrailProductArrayWrapper` (`GetAuditTrailMappers`), `AuditTrailTypesWrapper` (`GetAuditTrailTypes`), `EnabledModuleArrayWrapper` (`GetEnabledModules`), `SocketSettingsWrapper` (`GetSocketSettings`), `SsoSettingsConstantsWrapper` (`GetSsoSettingsV2Constants`), `AmazonS3RegionArrayWrapper` (`GetAmazonS3Regions`), `AccountEntryArrayWrapper` (`GetAccountsEntriesWith*Shared`)
- Other return types: `DeleteFileVersions` returns `FileOperationArrayWrapper`, `SearchUsersByQuery` returns `EmployeeFullArrayWrapper`, `ChangeDefaultAccessRights` returns `FileShareResponseArrayWrapper`, `UpdateCdnStorage` returns `StorageSettingsWrapper`, `GetAiPrices` returns `AiPricesWrapper`, `GetRestrictedAiModels` and `SetRestrictedAiModels` return `RestrictedAiModelsWrapper`; `DeleteGroup` and `DeleteRecent` return no body
- Renamed the AI operations after their tag, so the method names change: `CreateAgent` → `AiAgentsCreate`, `GetAgents` → `AiAgentsList`, `GetAgentInfo` → `AiAgentsGet`, `GetAgentsNewItems` → `AiAgentsNews`, `UpdateAgent` → `AiAgentsUpdate`, `DeleteAgent` → `AiAgentsDelete`, `UpdateAgentsQuota` → `AiAgentsUpdateQuota`, `ResetAgentsQuota` → `AiAgentsResetQuota`, `GetAiSettings` → `AiSettingsGet`, `GetAiUserSettings` → `AiSettingsGetUser`, `SetAiUserSettings` → `AiSettingsSetUser`, `GetVectorizationSettings` → `AiSettingsGetVectorization`, `SetVectorizationSettings` → `AiSettingsSetVectorization`, `StartTask` → `AiVectorizationStartTask`; in Files, `ChangeDownloadZipFromBody` → `ChangeDownloadZip`
- The AI agent operations take the agent `id` as a string and use their own `Ai`-prefixed models (`AiFolderWrapper`, `AiFolderContentWrapper`, `AiFileOperationWrapper`, `AiAgentsCreateRequest`, `AiAgentsUpdateRequest` and others) instead of the models of the Files API; `AiSettingsSetUser` and `AiSettingsSetVectorization` take the settings as a `Dictionary<string, object>`
- The OAuth 2.0 client and scope operations call `/api/2.0/oauth2/clients` and `/api/2.0/oauth2/scopes` instead of `/api/2.0/clients` and `/api/2.0/scopes`; `UpdateClientRequest` takes `redirect_uris` and `scopes`
- `CheckMoveOrCopyBatchItems` and `CheckMoveOrCopyDestFolder` take the batch as separate parameters (`folderIds`, `fileIds`, `destFolderId`, `conflictResolveType`, `deleteAfter`, `content`, `toFillOut`, `returnSingleOperation`) instead of one `inDto` object, which is how the server reads them
- Parameter changes: the date filters of `GetFileHistory`, `GetFolderHistory`, `GetAuditEventsByFilter` and `GetLoginEventsByFilter` are strings; `serviceName` of `GetCustomerOperations` and of `CustomerOperationsReportRequestDto` is a list; `SearchArea` is sent by name (`Active`, `Archive`, ...) instead of a number; `EditorConfigurationDto.encryptionKeys` is a list of `EncryptionKeyDto`; the `userid`, `productid` and `activationstatus` parameters are now `userId`, `productId` and `activationStatus`

### Removed

- Removed the superseded AI API classes `ChatApi`, `MCPApi`, `MessagesApi` and `ProvidersApi` from `src/DocSpace.API.SDK/Api/AI/` together with their models (`ChatDto`, `MessageDto`, `McpServerDto`, `AiProviderDto`, `ModelDto` and others); the web search settings moved from `GetWebSearchSettings`/`SetWebSearchSettings` to `WebSearchApi`
- Removed the request models `SetAiUserSettingsRequestDto` and `SetEmbeddingConfigRequestBody`, the `AiSettingsDto` properties that described the old AI chat (`webSearchEnabled`, `portalMcpServerId`, `modelAliases`, the `*ToolName` properties and others) and the model properties of `ChatSettingsDto` (`providerId`, `modelId`, `modelAlias`, `capabilities`, `multimodal`, `thinking`, `internal`)
- Removed `GetPrivacyFolder` and `IsAvailablePrivacyRoomSettings` (Files), `GetCustomerAiBalance` and `CreditAiBalance` (Payment)
- Removed the `subjectFilter` parameter of `GetRoomsFolder` and `AiAgentsList` with its `SubjectFilter` enum, the `MessageAction` values `PrivacyRoomEnable` and `PrivacyRoomDisable`, and the `public` property of the OAuth 2.0 client models
- The third-party accounts returned by `GetThirdPartyAccounts` no longer have the `auth_data` property, and the devices returned by `DocRegisterPusnNotificationDevice` and `SubscribeDocumentsPushNotification` no longer have `tenant`; neither was ever filled in

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
