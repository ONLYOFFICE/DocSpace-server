# GroupPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **string** |  | [optional] 
**Name** | Pointer to **string** |  | [optional] 
**CategoryID** | Pointer to **string** |  | [optional] 
**Parent** | Pointer to [**GroupPayload**](GroupPayload.md) |  | [optional] 
**Sid** | Pointer to **string** | LDAP identifier. REVIEW. | [optional] 
**Removed** | Pointer to **bool** |  | [optional] 

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

### GetCategoryID

`func (o *GroupPayload) GetCategoryID() string`

GetCategoryID returns the CategoryID field if non-nil, zero value otherwise.

### GetCategoryIDOk

`func (o *GroupPayload) GetCategoryIDOk() (*string, bool)`

GetCategoryIDOk returns a tuple with the CategoryID field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCategoryID

`func (o *GroupPayload) SetCategoryID(v string)`

SetCategoryID sets CategoryID field to given value.

### HasCategoryID

`func (o *GroupPayload) HasCategoryID() bool`

HasCategoryID returns a boolean if a field has been set.

### GetParent

`func (o *GroupPayload) GetParent() GroupPayload`

GetParent returns the Parent field if non-nil, zero value otherwise.

### GetParentOk

`func (o *GroupPayload) GetParentOk() (*GroupPayload, bool)`

GetParentOk returns a tuple with the Parent field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetParent

`func (o *GroupPayload) SetParent(v GroupPayload)`

SetParent sets Parent field to given value.

### HasParent

`func (o *GroupPayload) HasParent() bool`

HasParent returns a boolean if a field has been set.

### GetSid

`func (o *GroupPayload) GetSid() string`

GetSid returns the Sid field if non-nil, zero value otherwise.

### GetSidOk

`func (o *GroupPayload) GetSidOk() (*string, bool)`

GetSidOk returns a tuple with the Sid field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSid

`func (o *GroupPayload) SetSid(v string)`

SetSid sets Sid field to given value.

### HasSid

`func (o *GroupPayload) HasSid() bool`

HasSid returns a boolean if a field has been set.

### GetRemoved

`func (o *GroupPayload) GetRemoved() bool`

GetRemoved returns the Removed field if non-nil, zero value otherwise.

### GetRemovedOk

`func (o *GroupPayload) GetRemovedOk() (*bool, bool)`

GetRemovedOk returns a tuple with the Removed field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRemoved

`func (o *GroupPayload) SetRemoved(v bool)`

SetRemoved sets Removed field to given value.

### HasRemoved

`func (o *GroupPayload) HasRemoved() bool`

HasRemoved returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


