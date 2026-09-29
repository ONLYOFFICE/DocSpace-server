# RoomPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**RootFolderId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**Title** | Pointer to **string** |  | [optional] 
**FileEntryType** | Pointer to **int32** | 1 folder, 2 file. Present on every entry payload. | [optional] 
**Created** | Pointer to **time.Time** |  | [optional] 
**CreatedBy** | Pointer to [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**Updated** | Pointer to **time.Time** |  | [optional] 
**UpdatedBy** | Pointer to [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**RootFolderType** | Pointer to **int32** | enum FolderType | [optional] 
**ParentRoomType** | Pointer to **int32** | enum FolderType | [optional] 
**OriginId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginTitle** | Pointer to **string** |  | [optional] 
**OriginRoomTitle** | Pointer to **string** |  | [optional] 
**ProviderItem** | Pointer to **bool** |  | [optional] 
**ProviderKey** | Pointer to **string** |  | [optional] 
**ProviderId** | Pointer to **int32** |  | [optional] 
**Order** | Pointer to **int32** | Position within an indexed room. | [optional] 
**RoomType** | Pointer to **int32** | enum RoomType | [optional] 
**Type** | Pointer to **int32** | enum FolderType | [optional] 
**FilesCount** | Pointer to **int32** |  | [optional] 
**FoldersCount** | Pointer to **int32** |  | [optional] 
**Private** | Pointer to **bool** |  | [optional] 
**Indexing** | Pointer to **bool** |  | [optional] 
**DenyDownload** | Pointer to **bool** |  | [optional] 
**Pinned** | Pointer to **bool** |  | [optional] 
**QuotaLimit** | Pointer to **int64** |  | [optional] 
**UsedSpace** | Pointer to **int64** |  | [optional] 
**Color** | Pointer to **string** |  | [optional] 
**Cover** | Pointer to **string** |  | [optional] 

## Methods

### NewRoomPayload

`func NewRoomPayload() *RoomPayload`

NewRoomPayload instantiates a new RoomPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewRoomPayloadWithDefaults

`func NewRoomPayloadWithDefaults() *RoomPayload`

NewRoomPayloadWithDefaults instantiates a new RoomPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *RoomPayload) GetId() EntryId`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *RoomPayload) GetIdOk() (*EntryId, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *RoomPayload) SetId(v EntryId)`

SetId sets Id field to given value.

### HasId

`func (o *RoomPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetParentId

`func (o *RoomPayload) GetParentId() EntryId`

GetParentId returns the ParentId field if non-nil, zero value otherwise.

### GetParentIdOk

`func (o *RoomPayload) GetParentIdOk() (*EntryId, bool)`

GetParentIdOk returns a tuple with the ParentId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentId

`func (o *RoomPayload) SetParentId(v EntryId)`

SetParentId sets ParentId field to given value.

### HasParentId

`func (o *RoomPayload) HasParentId() bool`

HasParentId returns a boolean if a field has been set.

### GetRootFolderId

`func (o *RoomPayload) GetRootFolderId() EntryId`

GetRootFolderId returns the RootFolderId field if non-nil, zero value otherwise.

### GetRootFolderIdOk

`func (o *RoomPayload) GetRootFolderIdOk() (*EntryId, bool)`

GetRootFolderIdOk returns a tuple with the RootFolderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderId

`func (o *RoomPayload) SetRootFolderId(v EntryId)`

SetRootFolderId sets RootFolderId field to given value.

### HasRootFolderId

`func (o *RoomPayload) HasRootFolderId() bool`

HasRootFolderId returns a boolean if a field has been set.

### GetTitle

`func (o *RoomPayload) GetTitle() string`

GetTitle returns the Title field if non-nil, zero value otherwise.

### GetTitleOk

`func (o *RoomPayload) GetTitleOk() (*string, bool)`

GetTitleOk returns a tuple with the Title field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTitle

`func (o *RoomPayload) SetTitle(v string)`

SetTitle sets Title field to given value.

### HasTitle

`func (o *RoomPayload) HasTitle() bool`

HasTitle returns a boolean if a field has been set.

### GetFileEntryType

`func (o *RoomPayload) GetFileEntryType() int32`

GetFileEntryType returns the FileEntryType field if non-nil, zero value otherwise.

### GetFileEntryTypeOk

`func (o *RoomPayload) GetFileEntryTypeOk() (*int32, bool)`

GetFileEntryTypeOk returns a tuple with the FileEntryType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileEntryType

`func (o *RoomPayload) SetFileEntryType(v int32)`

SetFileEntryType sets FileEntryType field to given value.

### HasFileEntryType

`func (o *RoomPayload) HasFileEntryType() bool`

HasFileEntryType returns a boolean if a field has been set.

### GetCreated

`func (o *RoomPayload) GetCreated() time.Time`

GetCreated returns the Created field if non-nil, zero value otherwise.

### GetCreatedOk

`func (o *RoomPayload) GetCreatedOk() (*time.Time, bool)`

GetCreatedOk returns a tuple with the Created field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreated

`func (o *RoomPayload) SetCreated(v time.Time)`

SetCreated sets Created field to given value.

### HasCreated

`func (o *RoomPayload) HasCreated() bool`

HasCreated returns a boolean if a field has been set.

### GetCreatedBy

`func (o *RoomPayload) GetCreatedBy() UserSummaryPayload`

GetCreatedBy returns the CreatedBy field if non-nil, zero value otherwise.

### GetCreatedByOk

`func (o *RoomPayload) GetCreatedByOk() (*UserSummaryPayload, bool)`

GetCreatedByOk returns a tuple with the CreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreatedBy

`func (o *RoomPayload) SetCreatedBy(v UserSummaryPayload)`

SetCreatedBy sets CreatedBy field to given value.

### HasCreatedBy

`func (o *RoomPayload) HasCreatedBy() bool`

HasCreatedBy returns a boolean if a field has been set.

### GetUpdated

`func (o *RoomPayload) GetUpdated() time.Time`

GetUpdated returns the Updated field if non-nil, zero value otherwise.

### GetUpdatedOk

`func (o *RoomPayload) GetUpdatedOk() (*time.Time, bool)`

GetUpdatedOk returns a tuple with the Updated field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdated

`func (o *RoomPayload) SetUpdated(v time.Time)`

SetUpdated sets Updated field to given value.

### HasUpdated

`func (o *RoomPayload) HasUpdated() bool`

HasUpdated returns a boolean if a field has been set.

### GetUpdatedBy

`func (o *RoomPayload) GetUpdatedBy() UserSummaryPayload`

GetUpdatedBy returns the UpdatedBy field if non-nil, zero value otherwise.

### GetUpdatedByOk

`func (o *RoomPayload) GetUpdatedByOk() (*UserSummaryPayload, bool)`

GetUpdatedByOk returns a tuple with the UpdatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdatedBy

`func (o *RoomPayload) SetUpdatedBy(v UserSummaryPayload)`

SetUpdatedBy sets UpdatedBy field to given value.

### HasUpdatedBy

`func (o *RoomPayload) HasUpdatedBy() bool`

HasUpdatedBy returns a boolean if a field has been set.

### GetRootFolderType

`func (o *RoomPayload) GetRootFolderType() int32`

GetRootFolderType returns the RootFolderType field if non-nil, zero value otherwise.

### GetRootFolderTypeOk

`func (o *RoomPayload) GetRootFolderTypeOk() (*int32, bool)`

GetRootFolderTypeOk returns a tuple with the RootFolderType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderType

`func (o *RoomPayload) SetRootFolderType(v int32)`

SetRootFolderType sets RootFolderType field to given value.

### HasRootFolderType

`func (o *RoomPayload) HasRootFolderType() bool`

HasRootFolderType returns a boolean if a field has been set.

### GetParentRoomType

`func (o *RoomPayload) GetParentRoomType() int32`

GetParentRoomType returns the ParentRoomType field if non-nil, zero value otherwise.

### GetParentRoomTypeOk

`func (o *RoomPayload) GetParentRoomTypeOk() (*int32, bool)`

GetParentRoomTypeOk returns a tuple with the ParentRoomType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentRoomType

`func (o *RoomPayload) SetParentRoomType(v int32)`

SetParentRoomType sets ParentRoomType field to given value.

### HasParentRoomType

`func (o *RoomPayload) HasParentRoomType() bool`

HasParentRoomType returns a boolean if a field has been set.

### GetOriginId

`func (o *RoomPayload) GetOriginId() EntryId`

GetOriginId returns the OriginId field if non-nil, zero value otherwise.

### GetOriginIdOk

`func (o *RoomPayload) GetOriginIdOk() (*EntryId, bool)`

GetOriginIdOk returns a tuple with the OriginId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginId

`func (o *RoomPayload) SetOriginId(v EntryId)`

SetOriginId sets OriginId field to given value.

### HasOriginId

`func (o *RoomPayload) HasOriginId() bool`

HasOriginId returns a boolean if a field has been set.

### GetOriginRoomId

`func (o *RoomPayload) GetOriginRoomId() EntryId`

GetOriginRoomId returns the OriginRoomId field if non-nil, zero value otherwise.

### GetOriginRoomIdOk

`func (o *RoomPayload) GetOriginRoomIdOk() (*EntryId, bool)`

GetOriginRoomIdOk returns a tuple with the OriginRoomId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomId

`func (o *RoomPayload) SetOriginRoomId(v EntryId)`

SetOriginRoomId sets OriginRoomId field to given value.

### HasOriginRoomId

`func (o *RoomPayload) HasOriginRoomId() bool`

HasOriginRoomId returns a boolean if a field has been set.

### GetOriginTitle

`func (o *RoomPayload) GetOriginTitle() string`

GetOriginTitle returns the OriginTitle field if non-nil, zero value otherwise.

### GetOriginTitleOk

`func (o *RoomPayload) GetOriginTitleOk() (*string, bool)`

GetOriginTitleOk returns a tuple with the OriginTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginTitle

`func (o *RoomPayload) SetOriginTitle(v string)`

SetOriginTitle sets OriginTitle field to given value.

### HasOriginTitle

`func (o *RoomPayload) HasOriginTitle() bool`

HasOriginTitle returns a boolean if a field has been set.

### GetOriginRoomTitle

`func (o *RoomPayload) GetOriginRoomTitle() string`

GetOriginRoomTitle returns the OriginRoomTitle field if non-nil, zero value otherwise.

### GetOriginRoomTitleOk

`func (o *RoomPayload) GetOriginRoomTitleOk() (*string, bool)`

GetOriginRoomTitleOk returns a tuple with the OriginRoomTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomTitle

`func (o *RoomPayload) SetOriginRoomTitle(v string)`

SetOriginRoomTitle sets OriginRoomTitle field to given value.

### HasOriginRoomTitle

`func (o *RoomPayload) HasOriginRoomTitle() bool`

HasOriginRoomTitle returns a boolean if a field has been set.

### GetProviderItem

`func (o *RoomPayload) GetProviderItem() bool`

GetProviderItem returns the ProviderItem field if non-nil, zero value otherwise.

### GetProviderItemOk

`func (o *RoomPayload) GetProviderItemOk() (*bool, bool)`

GetProviderItemOk returns a tuple with the ProviderItem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderItem

`func (o *RoomPayload) SetProviderItem(v bool)`

SetProviderItem sets ProviderItem field to given value.

### HasProviderItem

`func (o *RoomPayload) HasProviderItem() bool`

HasProviderItem returns a boolean if a field has been set.

### GetProviderKey

`func (o *RoomPayload) GetProviderKey() string`

GetProviderKey returns the ProviderKey field if non-nil, zero value otherwise.

### GetProviderKeyOk

`func (o *RoomPayload) GetProviderKeyOk() (*string, bool)`

GetProviderKeyOk returns a tuple with the ProviderKey field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderKey

`func (o *RoomPayload) SetProviderKey(v string)`

SetProviderKey sets ProviderKey field to given value.

### HasProviderKey

`func (o *RoomPayload) HasProviderKey() bool`

HasProviderKey returns a boolean if a field has been set.

### GetProviderId

`func (o *RoomPayload) GetProviderId() int32`

GetProviderId returns the ProviderId field if non-nil, zero value otherwise.

### GetProviderIdOk

`func (o *RoomPayload) GetProviderIdOk() (*int32, bool)`

GetProviderIdOk returns a tuple with the ProviderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderId

`func (o *RoomPayload) SetProviderId(v int32)`

SetProviderId sets ProviderId field to given value.

### HasProviderId

`func (o *RoomPayload) HasProviderId() bool`

HasProviderId returns a boolean if a field has been set.

### GetOrder

`func (o *RoomPayload) GetOrder() int32`

GetOrder returns the Order field if non-nil, zero value otherwise.

### GetOrderOk

`func (o *RoomPayload) GetOrderOk() (*int32, bool)`

GetOrderOk returns a tuple with the Order field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOrder

`func (o *RoomPayload) SetOrder(v int32)`

SetOrder sets Order field to given value.

### HasOrder

`func (o *RoomPayload) HasOrder() bool`

HasOrder returns a boolean if a field has been set.

### GetRoomType

`func (o *RoomPayload) GetRoomType() int32`

GetRoomType returns the RoomType field if non-nil, zero value otherwise.

### GetRoomTypeOk

`func (o *RoomPayload) GetRoomTypeOk() (*int32, bool)`

GetRoomTypeOk returns a tuple with the RoomType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRoomType

`func (o *RoomPayload) SetRoomType(v int32)`

SetRoomType sets RoomType field to given value.

### HasRoomType

`func (o *RoomPayload) HasRoomType() bool`

HasRoomType returns a boolean if a field has been set.

### GetType

`func (o *RoomPayload) GetType() int32`

GetType returns the Type field if non-nil, zero value otherwise.

### GetTypeOk

`func (o *RoomPayload) GetTypeOk() (*int32, bool)`

GetTypeOk returns a tuple with the Type field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetType

`func (o *RoomPayload) SetType(v int32)`

SetType sets Type field to given value.

### HasType

`func (o *RoomPayload) HasType() bool`

HasType returns a boolean if a field has been set.

### GetFilesCount

`func (o *RoomPayload) GetFilesCount() int32`

GetFilesCount returns the FilesCount field if non-nil, zero value otherwise.

### GetFilesCountOk

`func (o *RoomPayload) GetFilesCountOk() (*int32, bool)`

GetFilesCountOk returns a tuple with the FilesCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFilesCount

`func (o *RoomPayload) SetFilesCount(v int32)`

SetFilesCount sets FilesCount field to given value.

### HasFilesCount

`func (o *RoomPayload) HasFilesCount() bool`

HasFilesCount returns a boolean if a field has been set.

### GetFoldersCount

`func (o *RoomPayload) GetFoldersCount() int32`

GetFoldersCount returns the FoldersCount field if non-nil, zero value otherwise.

### GetFoldersCountOk

`func (o *RoomPayload) GetFoldersCountOk() (*int32, bool)`

GetFoldersCountOk returns a tuple with the FoldersCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFoldersCount

`func (o *RoomPayload) SetFoldersCount(v int32)`

SetFoldersCount sets FoldersCount field to given value.

### HasFoldersCount

`func (o *RoomPayload) HasFoldersCount() bool`

HasFoldersCount returns a boolean if a field has been set.

### GetPrivate

`func (o *RoomPayload) GetPrivate() bool`

GetPrivate returns the Private field if non-nil, zero value otherwise.

### GetPrivateOk

`func (o *RoomPayload) GetPrivateOk() (*bool, bool)`

GetPrivateOk returns a tuple with the Private field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPrivate

`func (o *RoomPayload) SetPrivate(v bool)`

SetPrivate sets Private field to given value.

### HasPrivate

`func (o *RoomPayload) HasPrivate() bool`

HasPrivate returns a boolean if a field has been set.

### GetIndexing

`func (o *RoomPayload) GetIndexing() bool`

GetIndexing returns the Indexing field if non-nil, zero value otherwise.

### GetIndexingOk

`func (o *RoomPayload) GetIndexingOk() (*bool, bool)`

GetIndexingOk returns a tuple with the Indexing field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIndexing

`func (o *RoomPayload) SetIndexing(v bool)`

SetIndexing sets Indexing field to given value.

### HasIndexing

`func (o *RoomPayload) HasIndexing() bool`

HasIndexing returns a boolean if a field has been set.

### GetDenyDownload

`func (o *RoomPayload) GetDenyDownload() bool`

GetDenyDownload returns the DenyDownload field if non-nil, zero value otherwise.

### GetDenyDownloadOk

`func (o *RoomPayload) GetDenyDownloadOk() (*bool, bool)`

GetDenyDownloadOk returns a tuple with the DenyDownload field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetDenyDownload

`func (o *RoomPayload) SetDenyDownload(v bool)`

SetDenyDownload sets DenyDownload field to given value.

### HasDenyDownload

`func (o *RoomPayload) HasDenyDownload() bool`

HasDenyDownload returns a boolean if a field has been set.

### GetPinned

`func (o *RoomPayload) GetPinned() bool`

GetPinned returns the Pinned field if non-nil, zero value otherwise.

### GetPinnedOk

`func (o *RoomPayload) GetPinnedOk() (*bool, bool)`

GetPinnedOk returns a tuple with the Pinned field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPinned

`func (o *RoomPayload) SetPinned(v bool)`

SetPinned sets Pinned field to given value.

### HasPinned

`func (o *RoomPayload) HasPinned() bool`

HasPinned returns a boolean if a field has been set.

### GetQuotaLimit

`func (o *RoomPayload) GetQuotaLimit() int64`

GetQuotaLimit returns the QuotaLimit field if non-nil, zero value otherwise.

### GetQuotaLimitOk

`func (o *RoomPayload) GetQuotaLimitOk() (*int64, bool)`

GetQuotaLimitOk returns a tuple with the QuotaLimit field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetQuotaLimit

`func (o *RoomPayload) SetQuotaLimit(v int64)`

SetQuotaLimit sets QuotaLimit field to given value.

### HasQuotaLimit

`func (o *RoomPayload) HasQuotaLimit() bool`

HasQuotaLimit returns a boolean if a field has been set.

### GetUsedSpace

`func (o *RoomPayload) GetUsedSpace() int64`

GetUsedSpace returns the UsedSpace field if non-nil, zero value otherwise.

### GetUsedSpaceOk

`func (o *RoomPayload) GetUsedSpaceOk() (*int64, bool)`

GetUsedSpaceOk returns a tuple with the UsedSpace field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUsedSpace

`func (o *RoomPayload) SetUsedSpace(v int64)`

SetUsedSpace sets UsedSpace field to given value.

### HasUsedSpace

`func (o *RoomPayload) HasUsedSpace() bool`

HasUsedSpace returns a boolean if a field has been set.

### GetColor

`func (o *RoomPayload) GetColor() string`

GetColor returns the Color field if non-nil, zero value otherwise.

### GetColorOk

`func (o *RoomPayload) GetColorOk() (*string, bool)`

GetColorOk returns a tuple with the Color field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetColor

`func (o *RoomPayload) SetColor(v string)`

SetColor sets Color field to given value.

### HasColor

`func (o *RoomPayload) HasColor() bool`

HasColor returns a boolean if a field has been set.

### GetCover

`func (o *RoomPayload) GetCover() string`

GetCover returns the Cover field if non-nil, zero value otherwise.

### GetCoverOk

`func (o *RoomPayload) GetCoverOk() (*string, bool)`

GetCoverOk returns a tuple with the Cover field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCover

`func (o *RoomPayload) SetCover(v string)`

SetCover sets Cover field to given value.

### HasCover

`func (o *RoomPayload) HasCover() bool`

HasCover returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


