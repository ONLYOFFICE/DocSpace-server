

# FileEntryPayload

The payload for EVERY file, folder, room, agent and form trigger.  There is deliberately no File- or Folder-specific schema. WebhookManager calls PublishAsync<T1,T2> with a static parameter type of FileEntry<T>, so T1 binds to the abstract base and System.Text.Json serializes by DECLARED type. File<T> and Folder<T> members -- pureTitle, version, contentLength, folderType, filesCount, isRoom -- therefore never reach the wire, however the entry was published.  One consequence worth internalising: `title` IS present, even for files. File<T> hides Title behind [JsonIgnore] and exposes pureTitle instead, but that override is invisible here because the base declaration is what gets serialized.  Verified against a captured production delivery: all 14 keys of a real file.created payload are members of this schema and nothing else. (Files/Core/Core/Entries/FileEntry.cs; [JsonIgnore] members excluded.) 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | [**EntryId**](EntryId.md) |  |  [optional] |
|**parentId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**rootId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originRoomId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**folderIdDisplay** | [**EntryId**](EntryId.md) |  |  [optional] |
|**mutableId** | **Boolean** |  |  [optional] |
|**title** | **String** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  |  [optional] |
|**isNew** | **Boolean** | Declared abstract on FileEntry; omitted when false. |  [optional] |
|**createBy** | **UUID** |  |  [optional] |
|**createOn** | **OffsetDateTime** |  |  [optional] |
|**modifiedBy** | **UUID** |  |  [optional] |
|**modifiedOn** | **OffsetDateTime** |  |  [optional] |
|**sharedBy** | **UUID** |  |  [optional] |
|**rootCreateBy** | **UUID** |  |  [optional] |
|**parentRoomCreatedBy** | **UUID** |  |  [optional] |
|**rootFolderType** | **Integer** | enum FolderType |  [optional] |
|**parentRoomType** | **Integer** | enum FolderType |  [optional] |
|**fileEntryType** | **Integer** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  |  [optional] |
|**access** | **Integer** | enum FileShare |  [optional] |
|**shared** | **Boolean** |  |  [optional] |
|**sharedForUser** | **Boolean** |  |  [optional] |
|**sharedExternal** | **Boolean** |  |  [optional] |
|**parentShared** | **Boolean** |  |  [optional] |
|**providerId** | **Integer** |  |  [optional] |
|**providerKey** | **String** |  |  [optional] |
|**originTitle** | **String** |  |  [optional] |
|**originRoomTitle** | **String** |  |  [optional] |
|**order** | **Integer** |  |  [optional] |
|**error** | **String** |  |  [optional] |
|**tags** | **List&lt;Map&lt;String, Object&gt;&gt;** | TODO: expand Tag. |  [optional] |
|**shareRecord** | **Map&lt;String, Object&gt;** | TODO: expand FileShareRecord&lt;T&gt;. |  [optional] |
|**security** | **Map&lt;String, Boolean&gt;** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  |  [optional] |
|**securityByUsers** | **Map&lt;String, Map&lt;String, Boolean&gt;&gt;** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  |  [optional] |



