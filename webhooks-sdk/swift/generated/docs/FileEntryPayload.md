# FileEntryPayload

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parentId** | [**EntryId**](EntryId.md) |  | [optional] 
**rootId** | [**EntryId**](EntryId.md) |  | [optional] 
**originId** | [**EntryId**](EntryId.md) |  | [optional] 
**originRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**folderIdDisplay** | [**EntryId**](EntryId.md) |  | [optional] 
**mutableId** | **Bool** |  | [optional] 
**title** | **String** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  | [optional] 
**isNew** | **Bool** | Declared abstract on FileEntry; omitted when false. | [optional] 
**createBy** | **UUID** |  | [optional] 
**createOn** | **Date** |  | [optional] 
**modifiedBy** | **UUID** |  | [optional] 
**modifiedOn** | **Date** |  | [optional] 
**sharedBy** | **UUID** |  | [optional] 
**rootCreateBy** | **UUID** |  | [optional] 
**parentRoomCreatedBy** | **UUID** |  | [optional] 
**rootFolderType** | **Int** | enum FolderType | [optional] 
**parentRoomType** | **Int** | enum FolderType | [optional] 
**fileEntryType** | **Int** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  | [optional] 
**access** | **Int** | enum FileShare | [optional] 
**shared** | **Bool** |  | [optional] 
**sharedForUser** | **Bool** |  | [optional] 
**sharedExternal** | **Bool** |  | [optional] 
**parentShared** | **Bool** |  | [optional] 
**providerId** | **Int** |  | [optional] 
**providerKey** | **String** |  | [optional] 
**originTitle** | **String** |  | [optional] 
**originRoomTitle** | **String** |  | [optional] 
**order** | **Int** |  | [optional] 
**error** | **String** |  | [optional] 
**tags** | [[String: JSONValue]] | TODO: expand Tag. | [optional] 
**shareRecord** | **[String: JSONValue]** | TODO: expand FileShareRecord&lt;T&gt;. | [optional] 
**security** | **[String: Bool]** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  | [optional] 
**securityByUsers** | [String: [String: Bool]] | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


