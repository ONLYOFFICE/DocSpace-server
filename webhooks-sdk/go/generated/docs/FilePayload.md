# FilePayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**RootFolderId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**Title** | Pointer to **string** | The file entry title. | [optional] 
**FileEntryType** | Pointer to **int32** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. | [optional] 
**Created** | Pointer to **time.Time** | The UTC date when the file entry was created. | [optional] 
**CreatedBy** | Pointer to [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**Updated** | Pointer to **time.Time** | The UTC date when the file entry was last updated. | [optional] 
**UpdatedBy** | Pointer to [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**RootFolderType** | Pointer to **int32** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**ParentRoomType** | Pointer to **int32** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**OriginId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginTitle** | Pointer to **string** | The origin title of the file entry. | [optional] 
**OriginRoomTitle** | Pointer to **string** | The origin room title of the file entry. | [optional] 
**ProviderItem** | Pointer to **bool** | Specifies whether the file entry is backed by a third-party provider or not. | [optional] 
**ProviderKey** | Pointer to **string** | The third-party provider key of the file entry. | [optional] 
**ProviderId** | Pointer to **int32** | The third-party provider ID of the file entry. | [optional] 
**Order** | Pointer to **int32** | The position of the file entry within an indexed room. | [optional] 
**Version** | Pointer to **int32** | The file version. | [optional] 
**VersionGroup** | Pointer to **int32** | The version group of the file. | [optional] 
**ContentLength** | Pointer to **int64** | The file size in bytes, as a number; the REST FileDto sends a formatted string instead. | [optional] 
**FileType** | Pointer to **int32** | The file type. Values of &#x60;FileType&#x60;: 0 &#x3D; Unknown, 1 &#x3D; Archive, 2 &#x3D; Video, 3 &#x3D; Audio, 4 &#x3D; Image, 5 &#x3D; Spreadsheet, 6 &#x3D; Presentation, 7 &#x3D; Document, 10 &#x3D; Pdf, 11 &#x3D; Diagram. | [optional] 
**FileExst** | Pointer to **string** | The file extension, including the leading dot. | [optional] 
**Comment** | Pointer to **string** | The comment on the current file version. | [optional] 
**ViewUrl** | Pointer to **string** | The URL the file content can be downloaded from. Carries no share token. | [optional] 
**WebUrl** | Pointer to **string** | The URL the file can be opened at in a browser. Carries no share token. | [optional] 
**Encrypted** | Pointer to **bool** | Specifies whether the file is encrypted or not. | [optional] 
**Locked** | Pointer to **bool** | Specifies whether the file is locked or not. | [optional] 
**LockedBy** | Pointer to **string** | The user name of whoever locked the file. | [optional] 
**IsForm** | Pointer to **bool** | Specifies whether the file is a form or not. | [optional] 
**CustomFilterEnabled** | Pointer to **bool** | Specifies whether a custom filter is enabled for the file or not. | [optional] 
**CustomFilterEnabledBy** | Pointer to **string** | The user name of whoever enabled the custom filter. | [optional] 
**LastOpened** | Pointer to **time.Time** | The UTC date when the file was last opened. | [optional] 
**VectorizationStatus** | Pointer to **int32** | The AI vectorization status of the file. Values of &#x60;VectorizationStatus&#x60;: 0 &#x3D; InProgress, 1 &#x3D; Completed, 2 &#x3D; Failed. | [optional] 

## Methods

### NewFilePayload

`func NewFilePayload() *FilePayload`

NewFilePayload instantiates a new FilePayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFilePayloadWithDefaults

`func NewFilePayloadWithDefaults() *FilePayload`

NewFilePayloadWithDefaults instantiates a new FilePayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *FilePayload) GetId() EntryId`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *FilePayload) GetIdOk() (*EntryId, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *FilePayload) SetId(v EntryId)`

SetId sets Id field to given value.

### HasId

`func (o *FilePayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetParentId

`func (o *FilePayload) GetParentId() EntryId`

GetParentId returns the ParentId field if non-nil, zero value otherwise.

### GetParentIdOk

`func (o *FilePayload) GetParentIdOk() (*EntryId, bool)`

GetParentIdOk returns a tuple with the ParentId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentId

`func (o *FilePayload) SetParentId(v EntryId)`

SetParentId sets ParentId field to given value.

### HasParentId

`func (o *FilePayload) HasParentId() bool`

HasParentId returns a boolean if a field has been set.

### GetRootFolderId

`func (o *FilePayload) GetRootFolderId() EntryId`

GetRootFolderId returns the RootFolderId field if non-nil, zero value otherwise.

### GetRootFolderIdOk

`func (o *FilePayload) GetRootFolderIdOk() (*EntryId, bool)`

GetRootFolderIdOk returns a tuple with the RootFolderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderId

`func (o *FilePayload) SetRootFolderId(v EntryId)`

SetRootFolderId sets RootFolderId field to given value.

### HasRootFolderId

`func (o *FilePayload) HasRootFolderId() bool`

HasRootFolderId returns a boolean if a field has been set.

### GetTitle

`func (o *FilePayload) GetTitle() string`

GetTitle returns the Title field if non-nil, zero value otherwise.

### GetTitleOk

`func (o *FilePayload) GetTitleOk() (*string, bool)`

GetTitleOk returns a tuple with the Title field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTitle

`func (o *FilePayload) SetTitle(v string)`

SetTitle sets Title field to given value.

### HasTitle

`func (o *FilePayload) HasTitle() bool`

HasTitle returns a boolean if a field has been set.

### GetFileEntryType

`func (o *FilePayload) GetFileEntryType() int32`

GetFileEntryType returns the FileEntryType field if non-nil, zero value otherwise.

### GetFileEntryTypeOk

`func (o *FilePayload) GetFileEntryTypeOk() (*int32, bool)`

GetFileEntryTypeOk returns a tuple with the FileEntryType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileEntryType

`func (o *FilePayload) SetFileEntryType(v int32)`

SetFileEntryType sets FileEntryType field to given value.

### HasFileEntryType

`func (o *FilePayload) HasFileEntryType() bool`

HasFileEntryType returns a boolean if a field has been set.

### GetCreated

`func (o *FilePayload) GetCreated() time.Time`

GetCreated returns the Created field if non-nil, zero value otherwise.

### GetCreatedOk

`func (o *FilePayload) GetCreatedOk() (*time.Time, bool)`

GetCreatedOk returns a tuple with the Created field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreated

`func (o *FilePayload) SetCreated(v time.Time)`

SetCreated sets Created field to given value.

### HasCreated

`func (o *FilePayload) HasCreated() bool`

HasCreated returns a boolean if a field has been set.

### GetCreatedBy

`func (o *FilePayload) GetCreatedBy() UserSummaryPayload`

GetCreatedBy returns the CreatedBy field if non-nil, zero value otherwise.

### GetCreatedByOk

`func (o *FilePayload) GetCreatedByOk() (*UserSummaryPayload, bool)`

GetCreatedByOk returns a tuple with the CreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreatedBy

`func (o *FilePayload) SetCreatedBy(v UserSummaryPayload)`

SetCreatedBy sets CreatedBy field to given value.

### HasCreatedBy

`func (o *FilePayload) HasCreatedBy() bool`

HasCreatedBy returns a boolean if a field has been set.

### GetUpdated

`func (o *FilePayload) GetUpdated() time.Time`

GetUpdated returns the Updated field if non-nil, zero value otherwise.

### GetUpdatedOk

`func (o *FilePayload) GetUpdatedOk() (*time.Time, bool)`

GetUpdatedOk returns a tuple with the Updated field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdated

`func (o *FilePayload) SetUpdated(v time.Time)`

SetUpdated sets Updated field to given value.

### HasUpdated

`func (o *FilePayload) HasUpdated() bool`

HasUpdated returns a boolean if a field has been set.

### GetUpdatedBy

`func (o *FilePayload) GetUpdatedBy() UserSummaryPayload`

GetUpdatedBy returns the UpdatedBy field if non-nil, zero value otherwise.

### GetUpdatedByOk

`func (o *FilePayload) GetUpdatedByOk() (*UserSummaryPayload, bool)`

GetUpdatedByOk returns a tuple with the UpdatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdatedBy

`func (o *FilePayload) SetUpdatedBy(v UserSummaryPayload)`

SetUpdatedBy sets UpdatedBy field to given value.

### HasUpdatedBy

`func (o *FilePayload) HasUpdatedBy() bool`

HasUpdatedBy returns a boolean if a field has been set.

### GetRootFolderType

`func (o *FilePayload) GetRootFolderType() int32`

GetRootFolderType returns the RootFolderType field if non-nil, zero value otherwise.

### GetRootFolderTypeOk

`func (o *FilePayload) GetRootFolderTypeOk() (*int32, bool)`

GetRootFolderTypeOk returns a tuple with the RootFolderType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderType

`func (o *FilePayload) SetRootFolderType(v int32)`

SetRootFolderType sets RootFolderType field to given value.

### HasRootFolderType

`func (o *FilePayload) HasRootFolderType() bool`

HasRootFolderType returns a boolean if a field has been set.

### GetParentRoomType

`func (o *FilePayload) GetParentRoomType() int32`

GetParentRoomType returns the ParentRoomType field if non-nil, zero value otherwise.

### GetParentRoomTypeOk

`func (o *FilePayload) GetParentRoomTypeOk() (*int32, bool)`

GetParentRoomTypeOk returns a tuple with the ParentRoomType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentRoomType

`func (o *FilePayload) SetParentRoomType(v int32)`

SetParentRoomType sets ParentRoomType field to given value.

### HasParentRoomType

`func (o *FilePayload) HasParentRoomType() bool`

HasParentRoomType returns a boolean if a field has been set.

### GetOriginId

`func (o *FilePayload) GetOriginId() EntryId`

GetOriginId returns the OriginId field if non-nil, zero value otherwise.

### GetOriginIdOk

`func (o *FilePayload) GetOriginIdOk() (*EntryId, bool)`

GetOriginIdOk returns a tuple with the OriginId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginId

`func (o *FilePayload) SetOriginId(v EntryId)`

SetOriginId sets OriginId field to given value.

### HasOriginId

`func (o *FilePayload) HasOriginId() bool`

HasOriginId returns a boolean if a field has been set.

### GetOriginRoomId

`func (o *FilePayload) GetOriginRoomId() EntryId`

GetOriginRoomId returns the OriginRoomId field if non-nil, zero value otherwise.

### GetOriginRoomIdOk

`func (o *FilePayload) GetOriginRoomIdOk() (*EntryId, bool)`

GetOriginRoomIdOk returns a tuple with the OriginRoomId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomId

`func (o *FilePayload) SetOriginRoomId(v EntryId)`

SetOriginRoomId sets OriginRoomId field to given value.

### HasOriginRoomId

`func (o *FilePayload) HasOriginRoomId() bool`

HasOriginRoomId returns a boolean if a field has been set.

### GetOriginTitle

`func (o *FilePayload) GetOriginTitle() string`

GetOriginTitle returns the OriginTitle field if non-nil, zero value otherwise.

### GetOriginTitleOk

`func (o *FilePayload) GetOriginTitleOk() (*string, bool)`

GetOriginTitleOk returns a tuple with the OriginTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginTitle

`func (o *FilePayload) SetOriginTitle(v string)`

SetOriginTitle sets OriginTitle field to given value.

### HasOriginTitle

`func (o *FilePayload) HasOriginTitle() bool`

HasOriginTitle returns a boolean if a field has been set.

### GetOriginRoomTitle

`func (o *FilePayload) GetOriginRoomTitle() string`

GetOriginRoomTitle returns the OriginRoomTitle field if non-nil, zero value otherwise.

### GetOriginRoomTitleOk

`func (o *FilePayload) GetOriginRoomTitleOk() (*string, bool)`

GetOriginRoomTitleOk returns a tuple with the OriginRoomTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomTitle

`func (o *FilePayload) SetOriginRoomTitle(v string)`

SetOriginRoomTitle sets OriginRoomTitle field to given value.

### HasOriginRoomTitle

`func (o *FilePayload) HasOriginRoomTitle() bool`

HasOriginRoomTitle returns a boolean if a field has been set.

### GetProviderItem

`func (o *FilePayload) GetProviderItem() bool`

GetProviderItem returns the ProviderItem field if non-nil, zero value otherwise.

### GetProviderItemOk

`func (o *FilePayload) GetProviderItemOk() (*bool, bool)`

GetProviderItemOk returns a tuple with the ProviderItem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderItem

`func (o *FilePayload) SetProviderItem(v bool)`

SetProviderItem sets ProviderItem field to given value.

### HasProviderItem

`func (o *FilePayload) HasProviderItem() bool`

HasProviderItem returns a boolean if a field has been set.

### GetProviderKey

`func (o *FilePayload) GetProviderKey() string`

GetProviderKey returns the ProviderKey field if non-nil, zero value otherwise.

### GetProviderKeyOk

`func (o *FilePayload) GetProviderKeyOk() (*string, bool)`

GetProviderKeyOk returns a tuple with the ProviderKey field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderKey

`func (o *FilePayload) SetProviderKey(v string)`

SetProviderKey sets ProviderKey field to given value.

### HasProviderKey

`func (o *FilePayload) HasProviderKey() bool`

HasProviderKey returns a boolean if a field has been set.

### GetProviderId

`func (o *FilePayload) GetProviderId() int32`

GetProviderId returns the ProviderId field if non-nil, zero value otherwise.

### GetProviderIdOk

`func (o *FilePayload) GetProviderIdOk() (*int32, bool)`

GetProviderIdOk returns a tuple with the ProviderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderId

`func (o *FilePayload) SetProviderId(v int32)`

SetProviderId sets ProviderId field to given value.

### HasProviderId

`func (o *FilePayload) HasProviderId() bool`

HasProviderId returns a boolean if a field has been set.

### GetOrder

`func (o *FilePayload) GetOrder() int32`

GetOrder returns the Order field if non-nil, zero value otherwise.

### GetOrderOk

`func (o *FilePayload) GetOrderOk() (*int32, bool)`

GetOrderOk returns a tuple with the Order field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOrder

`func (o *FilePayload) SetOrder(v int32)`

SetOrder sets Order field to given value.

### HasOrder

`func (o *FilePayload) HasOrder() bool`

HasOrder returns a boolean if a field has been set.

### GetVersion

`func (o *FilePayload) GetVersion() int32`

GetVersion returns the Version field if non-nil, zero value otherwise.

### GetVersionOk

`func (o *FilePayload) GetVersionOk() (*int32, bool)`

GetVersionOk returns a tuple with the Version field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetVersion

`func (o *FilePayload) SetVersion(v int32)`

SetVersion sets Version field to given value.

### HasVersion

`func (o *FilePayload) HasVersion() bool`

HasVersion returns a boolean if a field has been set.

### GetVersionGroup

`func (o *FilePayload) GetVersionGroup() int32`

GetVersionGroup returns the VersionGroup field if non-nil, zero value otherwise.

### GetVersionGroupOk

`func (o *FilePayload) GetVersionGroupOk() (*int32, bool)`

GetVersionGroupOk returns a tuple with the VersionGroup field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetVersionGroup

`func (o *FilePayload) SetVersionGroup(v int32)`

SetVersionGroup sets VersionGroup field to given value.

### HasVersionGroup

`func (o *FilePayload) HasVersionGroup() bool`

HasVersionGroup returns a boolean if a field has been set.

### GetContentLength

`func (o *FilePayload) GetContentLength() int64`

GetContentLength returns the ContentLength field if non-nil, zero value otherwise.

### GetContentLengthOk

`func (o *FilePayload) GetContentLengthOk() (*int64, bool)`

GetContentLengthOk returns a tuple with the ContentLength field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetContentLength

`func (o *FilePayload) SetContentLength(v int64)`

SetContentLength sets ContentLength field to given value.

### HasContentLength

`func (o *FilePayload) HasContentLength() bool`

HasContentLength returns a boolean if a field has been set.

### GetFileType

`func (o *FilePayload) GetFileType() int32`

GetFileType returns the FileType field if non-nil, zero value otherwise.

### GetFileTypeOk

`func (o *FilePayload) GetFileTypeOk() (*int32, bool)`

GetFileTypeOk returns a tuple with the FileType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileType

`func (o *FilePayload) SetFileType(v int32)`

SetFileType sets FileType field to given value.

### HasFileType

`func (o *FilePayload) HasFileType() bool`

HasFileType returns a boolean if a field has been set.

### GetFileExst

`func (o *FilePayload) GetFileExst() string`

GetFileExst returns the FileExst field if non-nil, zero value otherwise.

### GetFileExstOk

`func (o *FilePayload) GetFileExstOk() (*string, bool)`

GetFileExstOk returns a tuple with the FileExst field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileExst

`func (o *FilePayload) SetFileExst(v string)`

SetFileExst sets FileExst field to given value.

### HasFileExst

`func (o *FilePayload) HasFileExst() bool`

HasFileExst returns a boolean if a field has been set.

### GetComment

`func (o *FilePayload) GetComment() string`

GetComment returns the Comment field if non-nil, zero value otherwise.

### GetCommentOk

`func (o *FilePayload) GetCommentOk() (*string, bool)`

GetCommentOk returns a tuple with the Comment field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetComment

`func (o *FilePayload) SetComment(v string)`

SetComment sets Comment field to given value.

### HasComment

`func (o *FilePayload) HasComment() bool`

HasComment returns a boolean if a field has been set.

### GetViewUrl

`func (o *FilePayload) GetViewUrl() string`

GetViewUrl returns the ViewUrl field if non-nil, zero value otherwise.

### GetViewUrlOk

`func (o *FilePayload) GetViewUrlOk() (*string, bool)`

GetViewUrlOk returns a tuple with the ViewUrl field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetViewUrl

`func (o *FilePayload) SetViewUrl(v string)`

SetViewUrl sets ViewUrl field to given value.

### HasViewUrl

`func (o *FilePayload) HasViewUrl() bool`

HasViewUrl returns a boolean if a field has been set.

### GetWebUrl

`func (o *FilePayload) GetWebUrl() string`

GetWebUrl returns the WebUrl field if non-nil, zero value otherwise.

### GetWebUrlOk

`func (o *FilePayload) GetWebUrlOk() (*string, bool)`

GetWebUrlOk returns a tuple with the WebUrl field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWebUrl

`func (o *FilePayload) SetWebUrl(v string)`

SetWebUrl sets WebUrl field to given value.

### HasWebUrl

`func (o *FilePayload) HasWebUrl() bool`

HasWebUrl returns a boolean if a field has been set.

### GetEncrypted

`func (o *FilePayload) GetEncrypted() bool`

GetEncrypted returns the Encrypted field if non-nil, zero value otherwise.

### GetEncryptedOk

`func (o *FilePayload) GetEncryptedOk() (*bool, bool)`

GetEncryptedOk returns a tuple with the Encrypted field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEncrypted

`func (o *FilePayload) SetEncrypted(v bool)`

SetEncrypted sets Encrypted field to given value.

### HasEncrypted

`func (o *FilePayload) HasEncrypted() bool`

HasEncrypted returns a boolean if a field has been set.

### GetLocked

`func (o *FilePayload) GetLocked() bool`

GetLocked returns the Locked field if non-nil, zero value otherwise.

### GetLockedOk

`func (o *FilePayload) GetLockedOk() (*bool, bool)`

GetLockedOk returns a tuple with the Locked field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLocked

`func (o *FilePayload) SetLocked(v bool)`

SetLocked sets Locked field to given value.

### HasLocked

`func (o *FilePayload) HasLocked() bool`

HasLocked returns a boolean if a field has been set.

### GetLockedBy

`func (o *FilePayload) GetLockedBy() string`

GetLockedBy returns the LockedBy field if non-nil, zero value otherwise.

### GetLockedByOk

`func (o *FilePayload) GetLockedByOk() (*string, bool)`

GetLockedByOk returns a tuple with the LockedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLockedBy

`func (o *FilePayload) SetLockedBy(v string)`

SetLockedBy sets LockedBy field to given value.

### HasLockedBy

`func (o *FilePayload) HasLockedBy() bool`

HasLockedBy returns a boolean if a field has been set.

### GetIsForm

`func (o *FilePayload) GetIsForm() bool`

GetIsForm returns the IsForm field if non-nil, zero value otherwise.

### GetIsFormOk

`func (o *FilePayload) GetIsFormOk() (*bool, bool)`

GetIsFormOk returns a tuple with the IsForm field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsForm

`func (o *FilePayload) SetIsForm(v bool)`

SetIsForm sets IsForm field to given value.

### HasIsForm

`func (o *FilePayload) HasIsForm() bool`

HasIsForm returns a boolean if a field has been set.

### GetCustomFilterEnabled

`func (o *FilePayload) GetCustomFilterEnabled() bool`

GetCustomFilterEnabled returns the CustomFilterEnabled field if non-nil, zero value otherwise.

### GetCustomFilterEnabledOk

`func (o *FilePayload) GetCustomFilterEnabledOk() (*bool, bool)`

GetCustomFilterEnabledOk returns a tuple with the CustomFilterEnabled field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCustomFilterEnabled

`func (o *FilePayload) SetCustomFilterEnabled(v bool)`

SetCustomFilterEnabled sets CustomFilterEnabled field to given value.

### HasCustomFilterEnabled

`func (o *FilePayload) HasCustomFilterEnabled() bool`

HasCustomFilterEnabled returns a boolean if a field has been set.

### GetCustomFilterEnabledBy

`func (o *FilePayload) GetCustomFilterEnabledBy() string`

GetCustomFilterEnabledBy returns the CustomFilterEnabledBy field if non-nil, zero value otherwise.

### GetCustomFilterEnabledByOk

`func (o *FilePayload) GetCustomFilterEnabledByOk() (*string, bool)`

GetCustomFilterEnabledByOk returns a tuple with the CustomFilterEnabledBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCustomFilterEnabledBy

`func (o *FilePayload) SetCustomFilterEnabledBy(v string)`

SetCustomFilterEnabledBy sets CustomFilterEnabledBy field to given value.

### HasCustomFilterEnabledBy

`func (o *FilePayload) HasCustomFilterEnabledBy() bool`

HasCustomFilterEnabledBy returns a boolean if a field has been set.

### GetLastOpened

`func (o *FilePayload) GetLastOpened() time.Time`

GetLastOpened returns the LastOpened field if non-nil, zero value otherwise.

### GetLastOpenedOk

`func (o *FilePayload) GetLastOpenedOk() (*time.Time, bool)`

GetLastOpenedOk returns a tuple with the LastOpened field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastOpened

`func (o *FilePayload) SetLastOpened(v time.Time)`

SetLastOpened sets LastOpened field to given value.

### HasLastOpened

`func (o *FilePayload) HasLastOpened() bool`

HasLastOpened returns a boolean if a field has been set.

### GetVectorizationStatus

`func (o *FilePayload) GetVectorizationStatus() int32`

GetVectorizationStatus returns the VectorizationStatus field if non-nil, zero value otherwise.

### GetVectorizationStatusOk

`func (o *FilePayload) GetVectorizationStatusOk() (*int32, bool)`

GetVectorizationStatusOk returns a tuple with the VectorizationStatus field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetVectorizationStatus

`func (o *FilePayload) SetVectorizationStatus(v int32)`

SetVectorizationStatus sets VectorizationStatus field to given value.

### HasVectorizationStatus

`func (o *FilePayload) HasVectorizationStatus() bool`

HasVectorizationStatus returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


