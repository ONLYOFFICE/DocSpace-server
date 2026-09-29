# FileEntryPayload

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

## Methods

### NewFileEntryPayload

`func NewFileEntryPayload() *FileEntryPayload`

NewFileEntryPayload instantiates a new FileEntryPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFileEntryPayloadWithDefaults

`func NewFileEntryPayloadWithDefaults() *FileEntryPayload`

NewFileEntryPayloadWithDefaults instantiates a new FileEntryPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *FileEntryPayload) GetId() EntryId`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *FileEntryPayload) GetIdOk() (*EntryId, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *FileEntryPayload) SetId(v EntryId)`

SetId sets Id field to given value.

### HasId

`func (o *FileEntryPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetParentId

`func (o *FileEntryPayload) GetParentId() EntryId`

GetParentId returns the ParentId field if non-nil, zero value otherwise.

### GetParentIdOk

`func (o *FileEntryPayload) GetParentIdOk() (*EntryId, bool)`

GetParentIdOk returns a tuple with the ParentId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentId

`func (o *FileEntryPayload) SetParentId(v EntryId)`

SetParentId sets ParentId field to given value.

### HasParentId

`func (o *FileEntryPayload) HasParentId() bool`

HasParentId returns a boolean if a field has been set.

### GetRootFolderId

`func (o *FileEntryPayload) GetRootFolderId() EntryId`

GetRootFolderId returns the RootFolderId field if non-nil, zero value otherwise.

### GetRootFolderIdOk

`func (o *FileEntryPayload) GetRootFolderIdOk() (*EntryId, bool)`

GetRootFolderIdOk returns a tuple with the RootFolderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderId

`func (o *FileEntryPayload) SetRootFolderId(v EntryId)`

SetRootFolderId sets RootFolderId field to given value.

### HasRootFolderId

`func (o *FileEntryPayload) HasRootFolderId() bool`

HasRootFolderId returns a boolean if a field has been set.

### GetTitle

`func (o *FileEntryPayload) GetTitle() string`

GetTitle returns the Title field if non-nil, zero value otherwise.

### GetTitleOk

`func (o *FileEntryPayload) GetTitleOk() (*string, bool)`

GetTitleOk returns a tuple with the Title field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTitle

`func (o *FileEntryPayload) SetTitle(v string)`

SetTitle sets Title field to given value.

### HasTitle

`func (o *FileEntryPayload) HasTitle() bool`

HasTitle returns a boolean if a field has been set.

### GetFileEntryType

`func (o *FileEntryPayload) GetFileEntryType() int32`

GetFileEntryType returns the FileEntryType field if non-nil, zero value otherwise.

### GetFileEntryTypeOk

`func (o *FileEntryPayload) GetFileEntryTypeOk() (*int32, bool)`

GetFileEntryTypeOk returns a tuple with the FileEntryType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFileEntryType

`func (o *FileEntryPayload) SetFileEntryType(v int32)`

SetFileEntryType sets FileEntryType field to given value.

### HasFileEntryType

`func (o *FileEntryPayload) HasFileEntryType() bool`

HasFileEntryType returns a boolean if a field has been set.

### GetCreated

`func (o *FileEntryPayload) GetCreated() time.Time`

GetCreated returns the Created field if non-nil, zero value otherwise.

### GetCreatedOk

`func (o *FileEntryPayload) GetCreatedOk() (*time.Time, bool)`

GetCreatedOk returns a tuple with the Created field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreated

`func (o *FileEntryPayload) SetCreated(v time.Time)`

SetCreated sets Created field to given value.

### HasCreated

`func (o *FileEntryPayload) HasCreated() bool`

HasCreated returns a boolean if a field has been set.

### GetCreatedBy

`func (o *FileEntryPayload) GetCreatedBy() UserSummaryPayload`

GetCreatedBy returns the CreatedBy field if non-nil, zero value otherwise.

### GetCreatedByOk

`func (o *FileEntryPayload) GetCreatedByOk() (*UserSummaryPayload, bool)`

GetCreatedByOk returns a tuple with the CreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreatedBy

`func (o *FileEntryPayload) SetCreatedBy(v UserSummaryPayload)`

SetCreatedBy sets CreatedBy field to given value.

### HasCreatedBy

`func (o *FileEntryPayload) HasCreatedBy() bool`

HasCreatedBy returns a boolean if a field has been set.

### GetUpdated

`func (o *FileEntryPayload) GetUpdated() time.Time`

GetUpdated returns the Updated field if non-nil, zero value otherwise.

### GetUpdatedOk

`func (o *FileEntryPayload) GetUpdatedOk() (*time.Time, bool)`

GetUpdatedOk returns a tuple with the Updated field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdated

`func (o *FileEntryPayload) SetUpdated(v time.Time)`

SetUpdated sets Updated field to given value.

### HasUpdated

`func (o *FileEntryPayload) HasUpdated() bool`

HasUpdated returns a boolean if a field has been set.

### GetUpdatedBy

`func (o *FileEntryPayload) GetUpdatedBy() UserSummaryPayload`

GetUpdatedBy returns the UpdatedBy field if non-nil, zero value otherwise.

### GetUpdatedByOk

`func (o *FileEntryPayload) GetUpdatedByOk() (*UserSummaryPayload, bool)`

GetUpdatedByOk returns a tuple with the UpdatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUpdatedBy

`func (o *FileEntryPayload) SetUpdatedBy(v UserSummaryPayload)`

SetUpdatedBy sets UpdatedBy field to given value.

### HasUpdatedBy

`func (o *FileEntryPayload) HasUpdatedBy() bool`

HasUpdatedBy returns a boolean if a field has been set.

### GetRootFolderType

`func (o *FileEntryPayload) GetRootFolderType() int32`

GetRootFolderType returns the RootFolderType field if non-nil, zero value otherwise.

### GetRootFolderTypeOk

`func (o *FileEntryPayload) GetRootFolderTypeOk() (*int32, bool)`

GetRootFolderTypeOk returns a tuple with the RootFolderType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootFolderType

`func (o *FileEntryPayload) SetRootFolderType(v int32)`

SetRootFolderType sets RootFolderType field to given value.

### HasRootFolderType

`func (o *FileEntryPayload) HasRootFolderType() bool`

HasRootFolderType returns a boolean if a field has been set.

### GetParentRoomType

`func (o *FileEntryPayload) GetParentRoomType() int32`

GetParentRoomType returns the ParentRoomType field if non-nil, zero value otherwise.

### GetParentRoomTypeOk

`func (o *FileEntryPayload) GetParentRoomTypeOk() (*int32, bool)`

GetParentRoomTypeOk returns a tuple with the ParentRoomType field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentRoomType

`func (o *FileEntryPayload) SetParentRoomType(v int32)`

SetParentRoomType sets ParentRoomType field to given value.

### HasParentRoomType

`func (o *FileEntryPayload) HasParentRoomType() bool`

HasParentRoomType returns a boolean if a field has been set.

### GetOriginId

`func (o *FileEntryPayload) GetOriginId() EntryId`

GetOriginId returns the OriginId field if non-nil, zero value otherwise.

### GetOriginIdOk

`func (o *FileEntryPayload) GetOriginIdOk() (*EntryId, bool)`

GetOriginIdOk returns a tuple with the OriginId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginId

`func (o *FileEntryPayload) SetOriginId(v EntryId)`

SetOriginId sets OriginId field to given value.

### HasOriginId

`func (o *FileEntryPayload) HasOriginId() bool`

HasOriginId returns a boolean if a field has been set.

### GetOriginRoomId

`func (o *FileEntryPayload) GetOriginRoomId() EntryId`

GetOriginRoomId returns the OriginRoomId field if non-nil, zero value otherwise.

### GetOriginRoomIdOk

`func (o *FileEntryPayload) GetOriginRoomIdOk() (*EntryId, bool)`

GetOriginRoomIdOk returns a tuple with the OriginRoomId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomId

`func (o *FileEntryPayload) SetOriginRoomId(v EntryId)`

SetOriginRoomId sets OriginRoomId field to given value.

### HasOriginRoomId

`func (o *FileEntryPayload) HasOriginRoomId() bool`

HasOriginRoomId returns a boolean if a field has been set.

### GetOriginTitle

`func (o *FileEntryPayload) GetOriginTitle() string`

GetOriginTitle returns the OriginTitle field if non-nil, zero value otherwise.

### GetOriginTitleOk

`func (o *FileEntryPayload) GetOriginTitleOk() (*string, bool)`

GetOriginTitleOk returns a tuple with the OriginTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginTitle

`func (o *FileEntryPayload) SetOriginTitle(v string)`

SetOriginTitle sets OriginTitle field to given value.

### HasOriginTitle

`func (o *FileEntryPayload) HasOriginTitle() bool`

HasOriginTitle returns a boolean if a field has been set.

### GetOriginRoomTitle

`func (o *FileEntryPayload) GetOriginRoomTitle() string`

GetOriginRoomTitle returns the OriginRoomTitle field if non-nil, zero value otherwise.

### GetOriginRoomTitleOk

`func (o *FileEntryPayload) GetOriginRoomTitleOk() (*string, bool)`

GetOriginRoomTitleOk returns a tuple with the OriginRoomTitle field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginRoomTitle

`func (o *FileEntryPayload) SetOriginRoomTitle(v string)`

SetOriginRoomTitle sets OriginRoomTitle field to given value.

### HasOriginRoomTitle

`func (o *FileEntryPayload) HasOriginRoomTitle() bool`

HasOriginRoomTitle returns a boolean if a field has been set.

### GetProviderItem

`func (o *FileEntryPayload) GetProviderItem() bool`

GetProviderItem returns the ProviderItem field if non-nil, zero value otherwise.

### GetProviderItemOk

`func (o *FileEntryPayload) GetProviderItemOk() (*bool, bool)`

GetProviderItemOk returns a tuple with the ProviderItem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderItem

`func (o *FileEntryPayload) SetProviderItem(v bool)`

SetProviderItem sets ProviderItem field to given value.

### HasProviderItem

`func (o *FileEntryPayload) HasProviderItem() bool`

HasProviderItem returns a boolean if a field has been set.

### GetProviderKey

`func (o *FileEntryPayload) GetProviderKey() string`

GetProviderKey returns the ProviderKey field if non-nil, zero value otherwise.

### GetProviderKeyOk

`func (o *FileEntryPayload) GetProviderKeyOk() (*string, bool)`

GetProviderKeyOk returns a tuple with the ProviderKey field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderKey

`func (o *FileEntryPayload) SetProviderKey(v string)`

SetProviderKey sets ProviderKey field to given value.

### HasProviderKey

`func (o *FileEntryPayload) HasProviderKey() bool`

HasProviderKey returns a boolean if a field has been set.

### GetProviderId

`func (o *FileEntryPayload) GetProviderId() int32`

GetProviderId returns the ProviderId field if non-nil, zero value otherwise.

### GetProviderIdOk

`func (o *FileEntryPayload) GetProviderIdOk() (*int32, bool)`

GetProviderIdOk returns a tuple with the ProviderId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProviderId

`func (o *FileEntryPayload) SetProviderId(v int32)`

SetProviderId sets ProviderId field to given value.

### HasProviderId

`func (o *FileEntryPayload) HasProviderId() bool`

HasProviderId returns a boolean if a field has been set.

### GetOrder

`func (o *FileEntryPayload) GetOrder() int32`

GetOrder returns the Order field if non-nil, zero value otherwise.

### GetOrderOk

`func (o *FileEntryPayload) GetOrderOk() (*int32, bool)`

GetOrderOk returns a tuple with the Order field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOrder

`func (o *FileEntryPayload) SetOrder(v int32)`

SetOrder sets Order field to given value.

### HasOrder

`func (o *FileEntryPayload) HasOrder() bool`

HasOrder returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


