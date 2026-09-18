# FileEntryPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**RootId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**FolderIdDisplay** | Pointer to [**EntryId**](EntryId.md) |  | [optional] 
**MutableId** | Pointer to **bool** |  | [optional] 
**Title** | Pointer to **string** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  | [optional] 
**IsNew** | Pointer to **bool** | Declared abstract on FileEntry; omitted when false. | [optional] 
**CreateBy** | Pointer to **string** |  | [optional] 
**CreateOn** | Pointer to **time.Time** |  | [optional] 
**ModifiedBy** | Pointer to **string** |  | [optional] 
**ModifiedOn** | Pointer to **time.Time** |  | [optional] 
**SharedBy** | Pointer to **string** |  | [optional] 
**RootCreateBy** | Pointer to **string** |  | [optional] 
**ParentRoomCreatedBy** | Pointer to **string** |  | [optional] 
**RootFolderType** | Pointer to **int32** | enum FolderType | [optional] 
**ParentRoomType** | Pointer to **int32** | enum FolderType | [optional] 
**FileEntryType** | Pointer to **int32** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  | [optional] 
**Access** | Pointer to **int32** | enum FileShare | [optional] 
**Shared** | Pointer to **bool** |  | [optional] 
**SharedForUser** | Pointer to **bool** |  | [optional] 
**SharedExternal** | Pointer to **bool** |  | [optional] 
**ParentShared** | Pointer to **bool** |  | [optional] 
**ProviderId** | Pointer to **int32** |  | [optional] 
**ProviderKey** | Pointer to **string** |  | [optional] 
**OriginTitle** | Pointer to **string** |  | [optional] 
**OriginRoomTitle** | Pointer to **string** |  | [optional] 
**Order** | Pointer to **int32** |  | [optional] 
**Error** | Pointer to **string** |  | [optional] 
**Tags** | Pointer to **[]map[string]interface{}** | TODO: expand Tag. | [optional] 
**ShareRecord** | Pointer to **map[string]interface{}** | TODO: expand FileShareRecord&lt;T&gt;. | [optional] 
**Security** | Pointer to **map[string]bool** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  | [optional] 
**SecurityByUsers** | Pointer to **map[string]map[string]bool** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  | [optional] 

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

### GetRootId

`func (o *FileEntryPayload) GetRootId() EntryId`

GetRootId returns the RootId field if non-nil, zero value otherwise.

### GetRootIdOk

`func (o *FileEntryPayload) GetRootIdOk() (*EntryId, bool)`

GetRootIdOk returns a tuple with the RootId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootId

`func (o *FileEntryPayload) SetRootId(v EntryId)`

SetRootId sets RootId field to given value.

### HasRootId

`func (o *FileEntryPayload) HasRootId() bool`

HasRootId returns a boolean if a field has been set.

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

### GetFolderIdDisplay

`func (o *FileEntryPayload) GetFolderIdDisplay() EntryId`

GetFolderIdDisplay returns the FolderIdDisplay field if non-nil, zero value otherwise.

### GetFolderIdDisplayOk

`func (o *FileEntryPayload) GetFolderIdDisplayOk() (*EntryId, bool)`

GetFolderIdDisplayOk returns a tuple with the FolderIdDisplay field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFolderIdDisplay

`func (o *FileEntryPayload) SetFolderIdDisplay(v EntryId)`

SetFolderIdDisplay sets FolderIdDisplay field to given value.

### HasFolderIdDisplay

`func (o *FileEntryPayload) HasFolderIdDisplay() bool`

HasFolderIdDisplay returns a boolean if a field has been set.

### GetMutableId

`func (o *FileEntryPayload) GetMutableId() bool`

GetMutableId returns the MutableId field if non-nil, zero value otherwise.

### GetMutableIdOk

`func (o *FileEntryPayload) GetMutableIdOk() (*bool, bool)`

GetMutableIdOk returns a tuple with the MutableId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetMutableId

`func (o *FileEntryPayload) SetMutableId(v bool)`

SetMutableId sets MutableId field to given value.

### HasMutableId

`func (o *FileEntryPayload) HasMutableId() bool`

HasMutableId returns a boolean if a field has been set.

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

### GetIsNew

`func (o *FileEntryPayload) GetIsNew() bool`

GetIsNew returns the IsNew field if non-nil, zero value otherwise.

### GetIsNewOk

`func (o *FileEntryPayload) GetIsNewOk() (*bool, bool)`

GetIsNewOk returns a tuple with the IsNew field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsNew

`func (o *FileEntryPayload) SetIsNew(v bool)`

SetIsNew sets IsNew field to given value.

### HasIsNew

`func (o *FileEntryPayload) HasIsNew() bool`

HasIsNew returns a boolean if a field has been set.

### GetCreateBy

`func (o *FileEntryPayload) GetCreateBy() string`

GetCreateBy returns the CreateBy field if non-nil, zero value otherwise.

### GetCreateByOk

`func (o *FileEntryPayload) GetCreateByOk() (*string, bool)`

GetCreateByOk returns a tuple with the CreateBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreateBy

`func (o *FileEntryPayload) SetCreateBy(v string)`

SetCreateBy sets CreateBy field to given value.

### HasCreateBy

`func (o *FileEntryPayload) HasCreateBy() bool`

HasCreateBy returns a boolean if a field has been set.

### GetCreateOn

`func (o *FileEntryPayload) GetCreateOn() time.Time`

GetCreateOn returns the CreateOn field if non-nil, zero value otherwise.

### GetCreateOnOk

`func (o *FileEntryPayload) GetCreateOnOk() (*time.Time, bool)`

GetCreateOnOk returns a tuple with the CreateOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreateOn

`func (o *FileEntryPayload) SetCreateOn(v time.Time)`

SetCreateOn sets CreateOn field to given value.

### HasCreateOn

`func (o *FileEntryPayload) HasCreateOn() bool`

HasCreateOn returns a boolean if a field has been set.

### GetModifiedBy

`func (o *FileEntryPayload) GetModifiedBy() string`

GetModifiedBy returns the ModifiedBy field if non-nil, zero value otherwise.

### GetModifiedByOk

`func (o *FileEntryPayload) GetModifiedByOk() (*string, bool)`

GetModifiedByOk returns a tuple with the ModifiedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetModifiedBy

`func (o *FileEntryPayload) SetModifiedBy(v string)`

SetModifiedBy sets ModifiedBy field to given value.

### HasModifiedBy

`func (o *FileEntryPayload) HasModifiedBy() bool`

HasModifiedBy returns a boolean if a field has been set.

### GetModifiedOn

`func (o *FileEntryPayload) GetModifiedOn() time.Time`

GetModifiedOn returns the ModifiedOn field if non-nil, zero value otherwise.

### GetModifiedOnOk

`func (o *FileEntryPayload) GetModifiedOnOk() (*time.Time, bool)`

GetModifiedOnOk returns a tuple with the ModifiedOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetModifiedOn

`func (o *FileEntryPayload) SetModifiedOn(v time.Time)`

SetModifiedOn sets ModifiedOn field to given value.

### HasModifiedOn

`func (o *FileEntryPayload) HasModifiedOn() bool`

HasModifiedOn returns a boolean if a field has been set.

### GetSharedBy

`func (o *FileEntryPayload) GetSharedBy() string`

GetSharedBy returns the SharedBy field if non-nil, zero value otherwise.

### GetSharedByOk

`func (o *FileEntryPayload) GetSharedByOk() (*string, bool)`

GetSharedByOk returns a tuple with the SharedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSharedBy

`func (o *FileEntryPayload) SetSharedBy(v string)`

SetSharedBy sets SharedBy field to given value.

### HasSharedBy

`func (o *FileEntryPayload) HasSharedBy() bool`

HasSharedBy returns a boolean if a field has been set.

### GetRootCreateBy

`func (o *FileEntryPayload) GetRootCreateBy() string`

GetRootCreateBy returns the RootCreateBy field if non-nil, zero value otherwise.

### GetRootCreateByOk

`func (o *FileEntryPayload) GetRootCreateByOk() (*string, bool)`

GetRootCreateByOk returns a tuple with the RootCreateBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRootCreateBy

`func (o *FileEntryPayload) SetRootCreateBy(v string)`

SetRootCreateBy sets RootCreateBy field to given value.

### HasRootCreateBy

`func (o *FileEntryPayload) HasRootCreateBy() bool`

HasRootCreateBy returns a boolean if a field has been set.

### GetParentRoomCreatedBy

`func (o *FileEntryPayload) GetParentRoomCreatedBy() string`

GetParentRoomCreatedBy returns the ParentRoomCreatedBy field if non-nil, zero value otherwise.

### GetParentRoomCreatedByOk

`func (o *FileEntryPayload) GetParentRoomCreatedByOk() (*string, bool)`

GetParentRoomCreatedByOk returns a tuple with the ParentRoomCreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentRoomCreatedBy

`func (o *FileEntryPayload) SetParentRoomCreatedBy(v string)`

SetParentRoomCreatedBy sets ParentRoomCreatedBy field to given value.

### HasParentRoomCreatedBy

`func (o *FileEntryPayload) HasParentRoomCreatedBy() bool`

HasParentRoomCreatedBy returns a boolean if a field has been set.

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

### GetAccess

`func (o *FileEntryPayload) GetAccess() int32`

GetAccess returns the Access field if non-nil, zero value otherwise.

### GetAccessOk

`func (o *FileEntryPayload) GetAccessOk() (*int32, bool)`

GetAccessOk returns a tuple with the Access field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetAccess

`func (o *FileEntryPayload) SetAccess(v int32)`

SetAccess sets Access field to given value.

### HasAccess

`func (o *FileEntryPayload) HasAccess() bool`

HasAccess returns a boolean if a field has been set.

### GetShared

`func (o *FileEntryPayload) GetShared() bool`

GetShared returns the Shared field if non-nil, zero value otherwise.

### GetSharedOk

`func (o *FileEntryPayload) GetSharedOk() (*bool, bool)`

GetSharedOk returns a tuple with the Shared field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetShared

`func (o *FileEntryPayload) SetShared(v bool)`

SetShared sets Shared field to given value.

### HasShared

`func (o *FileEntryPayload) HasShared() bool`

HasShared returns a boolean if a field has been set.

### GetSharedForUser

`func (o *FileEntryPayload) GetSharedForUser() bool`

GetSharedForUser returns the SharedForUser field if non-nil, zero value otherwise.

### GetSharedForUserOk

`func (o *FileEntryPayload) GetSharedForUserOk() (*bool, bool)`

GetSharedForUserOk returns a tuple with the SharedForUser field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSharedForUser

`func (o *FileEntryPayload) SetSharedForUser(v bool)`

SetSharedForUser sets SharedForUser field to given value.

### HasSharedForUser

`func (o *FileEntryPayload) HasSharedForUser() bool`

HasSharedForUser returns a boolean if a field has been set.

### GetSharedExternal

`func (o *FileEntryPayload) GetSharedExternal() bool`

GetSharedExternal returns the SharedExternal field if non-nil, zero value otherwise.

### GetSharedExternalOk

`func (o *FileEntryPayload) GetSharedExternalOk() (*bool, bool)`

GetSharedExternalOk returns a tuple with the SharedExternal field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSharedExternal

`func (o *FileEntryPayload) SetSharedExternal(v bool)`

SetSharedExternal sets SharedExternal field to given value.

### HasSharedExternal

`func (o *FileEntryPayload) HasSharedExternal() bool`

HasSharedExternal returns a boolean if a field has been set.

### GetParentShared

`func (o *FileEntryPayload) GetParentShared() bool`

GetParentShared returns the ParentShared field if non-nil, zero value otherwise.

### GetParentSharedOk

`func (o *FileEntryPayload) GetParentSharedOk() (*bool, bool)`

GetParentSharedOk returns a tuple with the ParentShared field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParentShared

`func (o *FileEntryPayload) SetParentShared(v bool)`

SetParentShared sets ParentShared field to given value.

### HasParentShared

`func (o *FileEntryPayload) HasParentShared() bool`

HasParentShared returns a boolean if a field has been set.

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

### GetError

`func (o *FileEntryPayload) GetError() string`

GetError returns the Error field if non-nil, zero value otherwise.

### GetErrorOk

`func (o *FileEntryPayload) GetErrorOk() (*string, bool)`

GetErrorOk returns a tuple with the Error field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetError

`func (o *FileEntryPayload) SetError(v string)`

SetError sets Error field to given value.

### HasError

`func (o *FileEntryPayload) HasError() bool`

HasError returns a boolean if a field has been set.

### GetTags

`func (o *FileEntryPayload) GetTags() []map[string]interface{}`

GetTags returns the Tags field if non-nil, zero value otherwise.

### GetTagsOk

`func (o *FileEntryPayload) GetTagsOk() (*[]map[string]interface{}, bool)`

GetTagsOk returns a tuple with the Tags field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTags

`func (o *FileEntryPayload) SetTags(v []map[string]interface{})`

SetTags sets Tags field to given value.

### HasTags

`func (o *FileEntryPayload) HasTags() bool`

HasTags returns a boolean if a field has been set.

### GetShareRecord

`func (o *FileEntryPayload) GetShareRecord() map[string]interface{}`

GetShareRecord returns the ShareRecord field if non-nil, zero value otherwise.

### GetShareRecordOk

`func (o *FileEntryPayload) GetShareRecordOk() (*map[string]interface{}, bool)`

GetShareRecordOk returns a tuple with the ShareRecord field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetShareRecord

`func (o *FileEntryPayload) SetShareRecord(v map[string]interface{})`

SetShareRecord sets ShareRecord field to given value.

### HasShareRecord

`func (o *FileEntryPayload) HasShareRecord() bool`

HasShareRecord returns a boolean if a field has been set.

### GetSecurity

`func (o *FileEntryPayload) GetSecurity() map[string]bool`

GetSecurity returns the Security field if non-nil, zero value otherwise.

### GetSecurityOk

`func (o *FileEntryPayload) GetSecurityOk() (*map[string]bool, bool)`

GetSecurityOk returns a tuple with the Security field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSecurity

`func (o *FileEntryPayload) SetSecurity(v map[string]bool)`

SetSecurity sets Security field to given value.

### HasSecurity

`func (o *FileEntryPayload) HasSecurity() bool`

HasSecurity returns a boolean if a field has been set.

### GetSecurityByUsers

`func (o *FileEntryPayload) GetSecurityByUsers() map[string]map[string]bool`

GetSecurityByUsers returns the SecurityByUsers field if non-nil, zero value otherwise.

### GetSecurityByUsersOk

`func (o *FileEntryPayload) GetSecurityByUsersOk() (*map[string]map[string]bool, bool)`

GetSecurityByUsersOk returns a tuple with the SecurityByUsers field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSecurityByUsers

`func (o *FileEntryPayload) SetSecurityByUsers(v map[string]map[string]bool)`

SetSecurityByUsers sets SecurityByUsers field to given value.

### HasSecurityByUsers

`func (o *FileEntryPayload) HasSecurityByUsers() bool`

HasSecurityByUsers returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


