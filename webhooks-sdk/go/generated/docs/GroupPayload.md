# GroupPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **string** |  | [optional] 
**Name** | Pointer to **string** |  | [optional] 
**Parent** | Pointer to **string** |  | [optional] 
**Category** | Pointer to **string** |  | [optional] 
**IsLDAP** | Pointer to **bool** |  | [optional] 
**IsSystem** | Pointer to **bool** |  | [optional] 
**Manager** | Pointer to [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**MembersCount** | Pointer to **int32** |  | [optional] 

## Methods

### NewGroupPayload

`func NewGroupPayload() *GroupPayload`

NewGroupPayload instantiates a new GroupPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewGroupPayloadWithDefaults

`func NewGroupPayloadWithDefaults() *GroupPayload`

NewGroupPayloadWithDefaults instantiates a new GroupPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *GroupPayload) GetId() string`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *GroupPayload) GetIdOk() (*string, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *GroupPayload) SetId(v string)`

SetId sets Id field to given value.

### HasId

`func (o *GroupPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetName

`func (o *GroupPayload) GetName() string`

GetName returns the Name field if non-nil, zero value otherwise.

### GetNameOk

`func (o *GroupPayload) GetNameOk() (*string, bool)`

GetNameOk returns a tuple with the Name field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetName

`func (o *GroupPayload) SetName(v string)`

SetName sets Name field to given value.

### HasName

`func (o *GroupPayload) HasName() bool`

HasName returns a boolean if a field has been set.

### GetParent

`func (o *GroupPayload) GetParent() string`

GetParent returns the Parent field if non-nil, zero value otherwise.

### GetParentOk

`func (o *GroupPayload) GetParentOk() (*string, bool)`

GetParentOk returns a tuple with the Parent field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParent

`func (o *GroupPayload) SetParent(v string)`

SetParent sets Parent field to given value.

### HasParent

`func (o *GroupPayload) HasParent() bool`

HasParent returns a boolean if a field has been set.

### GetCategory

`func (o *GroupPayload) GetCategory() string`

GetCategory returns the Category field if non-nil, zero value otherwise.

### GetCategoryOk

`func (o *GroupPayload) GetCategoryOk() (*string, bool)`

GetCategoryOk returns a tuple with the Category field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCategory

`func (o *GroupPayload) SetCategory(v string)`

SetCategory sets Category field to given value.

### HasCategory

`func (o *GroupPayload) HasCategory() bool`

HasCategory returns a boolean if a field has been set.

### GetIsLDAP

`func (o *GroupPayload) GetIsLDAP() bool`

GetIsLDAP returns the IsLDAP field if non-nil, zero value otherwise.

### GetIsLDAPOk

`func (o *GroupPayload) GetIsLDAPOk() (*bool, bool)`

GetIsLDAPOk returns a tuple with the IsLDAP field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsLDAP

`func (o *GroupPayload) SetIsLDAP(v bool)`

SetIsLDAP sets IsLDAP field to given value.

### HasIsLDAP

`func (o *GroupPayload) HasIsLDAP() bool`

HasIsLDAP returns a boolean if a field has been set.

### GetIsSystem

`func (o *GroupPayload) GetIsSystem() bool`

GetIsSystem returns the IsSystem field if non-nil, zero value otherwise.

### GetIsSystemOk

`func (o *GroupPayload) GetIsSystemOk() (*bool, bool)`

GetIsSystemOk returns a tuple with the IsSystem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsSystem

`func (o *GroupPayload) SetIsSystem(v bool)`

SetIsSystem sets IsSystem field to given value.

### HasIsSystem

`func (o *GroupPayload) HasIsSystem() bool`

HasIsSystem returns a boolean if a field has been set.

### GetManager

`func (o *GroupPayload) GetManager() UserSummaryPayload`

GetManager returns the Manager field if non-nil, zero value otherwise.

### GetManagerOk

`func (o *GroupPayload) GetManagerOk() (*UserSummaryPayload, bool)`

GetManagerOk returns a tuple with the Manager field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetManager

`func (o *GroupPayload) SetManager(v UserSummaryPayload)`

SetManager sets Manager field to given value.

### HasManager

`func (o *GroupPayload) HasManager() bool`

HasManager returns a boolean if a field has been set.

### GetMembersCount

`func (o *GroupPayload) GetMembersCount() int32`

GetMembersCount returns the MembersCount field if non-nil, zero value otherwise.

### GetMembersCountOk

`func (o *GroupPayload) GetMembersCountOk() (*int32, bool)`

GetMembersCountOk returns a tuple with the MembersCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetMembersCount

`func (o *GroupPayload) SetMembersCount(v int32)`

SetMembersCount sets MembersCount field to given value.

### HasMembersCount

`func (o *GroupPayload) HasMembersCount() bool`

HasMembersCount returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


