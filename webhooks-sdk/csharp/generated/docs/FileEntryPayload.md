# DocSpace.Webhooks.SDK.Model.FileEntryPayload
The payload for EVERY file, folder, room, agent and form trigger.  There is deliberately no File- or Folder-specific schema. WebhookManager calls PublishAsync<T1,T2> with a static parameter type of FileEntry<T>, so T1 binds to the abstract base and System.Text.Json serializes by DECLARED type. File<T> and Folder<T> members - - pureTitle, version, contentLength, folderType, filesCount, isRoom - - therefore never reach the wire, however the entry was published.  One consequence worth internalising: `title` IS present, even for files. File<T> hides Title behind [JsonIgnore] and exposes pureTitle instead, but that override is invisible here because the base declaration is what gets serialized.  Verified against a captured production delivery: all 14 keys of a real file.created payload are members of this schema and nothing else. (Files/Core/Core/Entries/FileEntry.cs; [JsonIgnore] members excluded.) 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | [**EntryId**](EntryId.md) |  | [optional] 
**RootId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**FolderIdDisplay** | [**EntryId**](EntryId.md) |  | [optional] 
**MutableId** | **bool** |  | [optional] 
**Title** | **string** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  | [optional] 
**IsNew** | **bool** | Declared abstract on FileEntry; omitted when false. | [optional] 
**CreateBy** | **Guid** |  | [optional] 
**CreateOn** | **DateTime** |  | [optional] 
**ModifiedBy** | **Guid** |  | [optional] 
**ModifiedOn** | **DateTime** |  | [optional] 
**SharedBy** | **Guid** |  | [optional] 
**RootCreateBy** | **Guid** |  | [optional] 
**ParentRoomCreatedBy** | **Guid** |  | [optional] 
**RootFolderType** | **int** | enum FolderType | [optional] 
**ParentRoomType** | **int** | enum FolderType | [optional] 
**FileEntryType** | **int** | enum FileEntryType - - 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  | [optional] 
**Access** | **int** | enum FileShare | [optional] 
**Shared** | **bool** |  | [optional] 
**SharedForUser** | **bool** |  | [optional] 
**SharedExternal** | **bool** |  | [optional] 
**ParentShared** | **bool** |  | [optional] 
**ProviderId** | **int** |  | [optional] 
**ProviderKey** | **string** |  | [optional] 
**OriginTitle** | **string** |  | [optional] 
**OriginRoomTitle** | **string** |  | [optional] 
**Order** | **int** |  | [optional] 
**Error** | **string** |  | [optional] 
**Tags** | **List&lt;Dictionary&lt;string, Object&gt;&gt;** | TODO: expand Tag. | [optional] 
**ShareRecord** | **Dictionary&lt;string, Object&gt;** | TODO: expand FileShareRecord&lt;T&gt;. | [optional] 
**Security** | **Dictionary&lt;string, bool&gt;** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  | [optional] 
**SecurityByUsers** | **Dictionary&lt;string, Dictionary&lt;string, bool&gt;&gt;** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

