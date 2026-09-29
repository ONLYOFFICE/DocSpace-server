# FolderPayload

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
**FilesCount** | Pointer to **int32** |  | [optional] 
**FoldersCount** | Pointer to **int32** |  | [optional] 
**Type** | Pointer to **int32** | enum FolderType | [optional] 
**IsShareable** | Pointer to **bool** |  | [optional] 

## Methods

### NewFolderPayload

`func NewFolderPayload() *FolderPayload`

NewFolderPayload instantiates a new FolderPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFolderPayloadWithDefaults

`func NewFolderPayloadWithDefaults() *FolderPayload`

NewFolderPayloadWithDefaults instantiates a new FolderPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *FolderPayload) GetId() EntryId`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *FolderPayload) GetIdOk() (*EntryId, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *FolderPayload) SetId(v EntryId)`

SetId sets Id field to given value.

### HasId

`func (o *FolderPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetParentId

`func (o *FolderPayload) GetParentId() EntryId`

GetParentId returns the ParentId field if non-nil, zero value otherwise.

### GetParentIdOk

`func (o *FolderPayload) GetParentIdOk() (*EntryId, bool)`

GetParentIdOk returns a tuple with the ParentId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentId

`func (o *FolderPayload) SetParentId(v EntryId)`

SetParentId sets ParentId field to given value.

### HasParentId

`func (o *FolderPayload) HasParentId() bool`

HasParentId returns a boolean if a field has been set.

### GetRootFolderId

`func (o *FolderPayload) GetRootFolderId() EntryId`

GetRootFolderId returns the RootFolderId field if non-nil, zero value otherwise.

### GetRootFolderIdOk

`func (o *FolderPayload) GetRootFolderIdOk() (*EntryId, bool)`

GetRootFolderIdOk returns a tuple with the RootFolderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderId

`func (o *FolderPayload) SetRootFolderId(v EntryId)`

SetRootFolderId sets RootFolderId field to given value.

### HasRootFolderId

`func (o *FolderPayload) HasRootFolderId() bool`

HasRootFolderId returns a boolean if a field has been set.

### GetTitle

`func (o *FolderPayload) GetTitle() string`

GetTitle returns the Title field if non-nil, zero value otherwise.

### GetTitleOk

`func (o *FolderPayload) GetTitleOk() (*string, bool)`

GetTitleOk returns a tuple with the Title field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTitle

`func (o *FolderPayload) SetTitle(v string)`

SetTitle sets Title field to given value.

### HasTitle

`func (o *FolderPayload) HasTitle() bool`

HasTitle returns a boolean if a field has been set.

### GetFileEntryType

`func (o *FolderPayload) GetFileEntryType() int32`

GetFileEntryType returns the FileEntryType field if non-nil, zero value otherwise.

### GetFileEntryTypeOk

`func (o *FolderPayload) GetFileEntryTypeOk() (*int32, bool)`

GetFileEntryTypeOk returns a tuple with the FileEntryType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileEntryType

`func (o *FolderPayload) SetFileEntryType(v int32)`

SetFileEntryType sets FileEntryType field to given value.

### HasFileEntryType

`func (o *FolderPayload) HasFileEntryType() bool`

HasFileEntryType returns a boolean if a field has been set.

### GetCreated

`func (o *FolderPayload) GetCreated() time.Time`

GetCreated returns the Created field if non-nil, zero value otherwise.

### GetCreatedOk

`func (o *FolderPayload) GetCreatedOk() (*time.Time, bool)`

GetCreatedOk returns a tuple with the Created field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreated

`func (o *FolderPayload) SetCreated(v time.Time)`

SetCreated sets Created field to given value.

### HasCreated

`func (o *FolderPayload) HasCreated() bool`

HasCreated returns a boolean if a field has been set.

### GetCreatedBy

`func (o *FolderPayload) GetCreatedBy() UserSummaryPayload`

GetCreatedBy returns the CreatedBy field if non-nil, zero value otherwise.

### GetCreatedByOk

`func (o *FolderPayload) GetCreatedByOk() (*UserSummaryPayload, bool)`

GetCreatedByOk returns a tuple with the CreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreatedBy

`func (o *FolderPayload) SetCreatedBy(v UserSummaryPayload)`

SetCreatedBy sets CreatedBy field to given value.

### HasCreatedBy

`func (o *FolderPayload) HasCreatedBy() bool`

HasCreatedBy returns a boolean if a field has been set.

### GetUpdated

`func (o *FolderPayload) GetUpdated() time.Time`

GetUpdated returns the Updated field if non-nil, zero value otherwise.

### GetUpdatedOk

`func (o *FolderPayload) GetUpdatedOk() (*time.Time, bool)`

GetUpdatedOk returns a tuple with the Updated field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdated

`func (o *FolderPayload) SetUpdated(v time.Time)`

SetUpdated sets Updated field to given value.

### HasUpdated

`func (o *FolderPayload) HasUpdated() bool`

HasUpdated returns a boolean if a field has been set.

### GetUpdatedBy

`func (o *FolderPayload) GetUpdatedBy() UserSummaryPayload`

GetUpdatedBy returns the UpdatedBy field if non-nil, zero value otherwise.

### GetUpdatedByOk

`func (o *FolderPayload) GetUpdatedByOk() (*UserSummaryPayload, bool)`

GetUpdatedByOk returns a tuple with the UpdatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdatedBy

`func (o *FolderPayload) SetUpdatedBy(v UserSummaryPayload)`

SetUpdatedBy sets UpdatedBy field to given value.

### HasUpdatedBy

`func (o *FolderPayload) HasUpdatedBy() bool`

HasUpdatedBy returns a boolean if a field has been set.

### GetRootFolderType

`func (o *FolderPayload) GetRootFolderType() int32`

GetRootFolderType returns the RootFolderType field if non-nil, zero value otherwise.

### GetRootFolderTypeOk

`func (o *FolderPayload) GetRootFolderTypeOk() (*int32, bool)`

GetRootFolderTypeOk returns a tuple with the RootFolderType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderType

`func (o *FolderPayload) SetRootFolderType(v int32)`

SetRootFolderType sets RootFolderType field to given value.

### HasRootFolderType

`func (o *FolderPayload) HasRootFolderType() bool`

HasRootFolderType returns a boolean if a field has been set.

### GetParentRoomType

`func (o *FolderPayload) GetParentRoomType() int32`

GetParentRoomType returns the ParentRoomType field if non-nil, zero value otherwise.

### GetParentRoomTypeOk

`func (o *FolderPayload) GetParentRoomTypeOk() (*int32, bool)`

GetParentRoomTypeOk returns a tuple with the ParentRoomType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentRoomType

`func (o *FolderPayload) SetParentRoomType(v int32)`

SetParentRoomType sets ParentRoomType field to given value.

### HasParentRoomType

`func (o *FolderPayload) HasParentRoomType() bool`

HasParentRoomType returns a boolean if a field has been set.

### GetOriginId

`func (o *FolderPayload) GetOriginId() EntryId`

GetOriginId returns the OriginId field if non-nil, zero value otherwise.

### GetOriginIdOk

`func (o *FolderPayload) GetOriginIdOk() (*EntryId, bool)`

GetOriginIdOk returns a tuple with the OriginId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginId

`func (o *FolderPayload) SetOriginId(v EntryId)`

SetOriginId sets OriginId field to given value.

### HasOriginId

`func (o *FolderPayload) HasOriginId() bool`

HasOriginId returns a boolean if a field has been set.

### GetOriginRoomId

`func (o *FolderPayload) GetOriginRoomId() EntryId`

GetOriginRoomId returns the OriginRoomId field if non-nil, zero value otherwise.

### GetOriginRoomIdOk

`func (o *FolderPayload) GetOriginRoomIdOk() (*EntryId, bool)`

GetOriginRoomIdOk returns a tuple with the OriginRoomId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomId

`func (o *FolderPayload) SetOriginRoomId(v EntryId)`

SetOriginRoomId sets OriginRoomId field to given value.

### HasOriginRoomId

`func (o *FolderPayload) HasOriginRoomId() bool`

HasOriginRoomId returns a boolean if a field has been set.

### GetOriginTitle

`func (o *FolderPayload) GetOriginTitle() string`

GetOriginTitle returns the OriginTitle field if non-nil, zero value otherwise.

### GetOriginTitleOk

`func (o *FolderPayload) GetOriginTitleOk() (*string, bool)`

GetOriginTitleOk returns a tuple with the OriginTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginTitle

`func (o *FolderPayload) SetOriginTitle(v string)`

SetOriginTitle sets OriginTitle field to given value.

### HasOriginTitle

`func (o *FolderPayload) HasOriginTitle() bool`

HasOriginTitle returns a boolean if a field has been set.

### GetOriginRoomTitle

`func (o *FolderPayload) GetOriginRoomTitle() string`

GetOriginRoomTitle returns the OriginRoomTitle field if non-nil, zero value otherwise.

### GetOriginRoomTitleOk

`func (o *FolderPayload) GetOriginRoomTitleOk() (*string, bool)`

GetOriginRoomTitleOk returns a tuple with the OriginRoomTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomTitle

`func (o *FolderPayload) SetOriginRoomTitle(v string)`

SetOriginRoomTitle sets OriginRoomTitle field to given value.

### HasOriginRoomTitle

`func (o *FolderPayload) HasOriginRoomTitle() bool`

HasOriginRoomTitle returns a boolean if a field has been set.

### GetProviderItem

`func (o *FolderPayload) GetProviderItem() bool`

GetProviderItem returns the ProviderItem field if non-nil, zero value otherwise.

### GetProviderItemOk

`func (o *FolderPayload) GetProviderItemOk() (*bool, bool)`

GetProviderItemOk returns a tuple with the ProviderItem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderItem

`func (o *FolderPayload) SetProviderItem(v bool)`

SetProviderItem sets ProviderItem field to given value.

### HasProviderItem

`func (o *FolderPayload) HasProviderItem() bool`

HasProviderItem returns a boolean if a field has been set.

### GetProviderKey

`func (o *FolderPayload) GetProviderKey() string`

GetProviderKey returns the ProviderKey field if non-nil, zero value otherwise.

### GetProviderKeyOk

`func (o *FolderPayload) GetProviderKeyOk() (*string, bool)`

GetProviderKeyOk returns a tuple with the ProviderKey field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderKey

`func (o *FolderPayload) SetProviderKey(v string)`

SetProviderKey sets ProviderKey field to given value.

### HasProviderKey

`func (o *FolderPayload) HasProviderKey() bool`

HasProviderKey returns a boolean if a field has been set.

### GetProviderId

`func (o *FolderPayload) GetProviderId() int32`

GetProviderId returns the ProviderId field if non-nil, zero value otherwise.

### GetProviderIdOk

`func (o *FolderPayload) GetProviderIdOk() (*int32, bool)`

GetProviderIdOk returns a tuple with the ProviderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderId

`func (o *FolderPayload) SetProviderId(v int32)`

SetProviderId sets ProviderId field to given value.

### HasProviderId

`func (o *FolderPayload) HasProviderId() bool`

HasProviderId returns a boolean if a field has been set.

### GetOrder

`func (o *FolderPayload) GetOrder() int32`

GetOrder returns the Order field if non-nil, zero value otherwise.

### GetOrderOk

`func (o *FolderPayload) GetOrderOk() (*int32, bool)`

GetOrderOk returns a tuple with the Order field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOrder

`func (o *FolderPayload) SetOrder(v int32)`

SetOrder sets Order field to given value.

### HasOrder

`func (o *FolderPayload) HasOrder() bool`

HasOrder returns a boolean if a field has been set.

### GetFilesCount

`func (o *FolderPayload) GetFilesCount() int32`

GetFilesCount returns the FilesCount field if non-nil, zero value otherwise.

### GetFilesCountOk

`func (o *FolderPayload) GetFilesCountOk() (*int32, bool)`

GetFilesCountOk returns a tuple with the FilesCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFilesCount

`func (o *FolderPayload) SetFilesCount(v int32)`

SetFilesCount sets FilesCount field to given value.

### HasFilesCount

`func (o *FolderPayload) HasFilesCount() bool`

HasFilesCount returns a boolean if a field has been set.

### GetFoldersCount

`func (o *FolderPayload) GetFoldersCount() int32`

GetFoldersCount returns the FoldersCount field if non-nil, zero value otherwise.

### GetFoldersCountOk

`func (o *FolderPayload) GetFoldersCountOk() (*int32, bool)`

GetFoldersCountOk returns a tuple with the FoldersCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFoldersCount

`func (o *FolderPayload) SetFoldersCount(v int32)`

SetFoldersCount sets FoldersCount field to given value.

### HasFoldersCount

`func (o *FolderPayload) HasFoldersCount() bool`

HasFoldersCount returns a boolean if a field has been set.

### GetType

`func (o *FolderPayload) GetType() int32`

GetType returns the Type field if non-nil, zero value otherwise.

### GetTypeOk

`func (o *FolderPayload) GetTypeOk() (*int32, bool)`

GetTypeOk returns a tuple with the Type field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetType

`func (o *FolderPayload) SetType(v int32)`

SetType sets Type field to given value.

### HasType

`func (o *FolderPayload) HasType() bool`

HasType returns a boolean if a field has been set.

### GetIsShareable

`func (o *FolderPayload) GetIsShareable() bool`

GetIsShareable returns the IsShareable field if non-nil, zero value otherwise.

### GetIsShareableOk

`func (o *FolderPayload) GetIsShareableOk() (*bool, bool)`

GetIsShareableOk returns a tuple with the IsShareable field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsShareable

`func (o *FolderPayload) SetIsShareable(v bool)`

SetIsShareable sets IsShareable field to given value.

### HasIsShareable

`func (o *FolderPayload) HasIsShareable() bool`

HasIsShareable returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


