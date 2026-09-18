
# FileEntryPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**EntryId**](EntryId.md) |  |  [optional] |
| **parentId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **rootId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **originId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **originRoomId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **folderIdDisplay** | [**EntryId**](EntryId.md) |  |  [optional] |
| **mutableId** | **kotlin.Boolean** |  |  [optional] |
| **title** | **kotlin.String** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  |  [optional] |
| **isNew** | **kotlin.Boolean** | Declared abstract on FileEntry; omitted when false. |  [optional] |
| **createBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **createOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **modifiedBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **modifiedOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **sharedBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **rootCreateBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **parentRoomCreatedBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **rootFolderType** | **kotlin.Int** | enum FolderType |  [optional] |
| **parentRoomType** | **kotlin.Int** | enum FolderType |  [optional] |
| **fileEntryType** | **kotlin.Int** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  |  [optional] |
| **access** | **kotlin.Int** | enum FileShare |  [optional] |
| **shared** | **kotlin.Boolean** |  |  [optional] |
| **sharedForUser** | **kotlin.Boolean** |  |  [optional] |
| **sharedExternal** | **kotlin.Boolean** |  |  [optional] |
| **parentShared** | **kotlin.Boolean** |  |  [optional] |
| **providerId** | **kotlin.Int** |  |  [optional] |
| **providerKey** | **kotlin.String** |  |  [optional] |
| **originTitle** | **kotlin.String** |  |  [optional] |
| **originRoomTitle** | **kotlin.String** |  |  [optional] |
| **order** | **kotlin.Int** |  |  [optional] |
| **error** | **kotlin.String** |  |  [optional] |
| **tags** | **kotlin.collections.List&lt;kotlin.collections.Map&lt;kotlin.String, kotlin.Any&gt;&gt;** | TODO: expand Tag. |  [optional] |
| **shareRecord** | [**kotlin.collections.Map&lt;kotlin.String, kotlin.Any&gt;**](kotlin.Any.md) | TODO: expand FileShareRecord&lt;T&gt;. |  [optional] |
| **security** | **kotlin.collections.Map&lt;kotlin.String, kotlin.Boolean&gt;** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  |  [optional] |
| **securityByUsers** | **kotlin.collections.Map&lt;kotlin.String, kotlin.collections.Map&lt;kotlin.String, kotlin.Boolean&gt;&gt;** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  |  [optional] |



