# GroupSummaryPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **string** |  | [optional] 
**Name** | Pointer to **string** |  | [optional] 
**Manager** | Pointer to **string** | The user name of the group manager, not an id. | [optional] 
**IsSystem** | Pointer to **bool** |  | [optional] 

## Methods

### NewGroupSummaryPayload

`func NewGroupSummaryPayload() *GroupSummaryPayload`

NewGroupSummaryPayload instantiates a new GroupSummaryPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewGroupSummaryPayloadWithDefaults

`func NewGroupSummaryPayloadWithDefaults() *GroupSummaryPayload`

NewGroupSummaryPayloadWithDefaults instantiates a new GroupSummaryPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *GroupSummaryPayload) GetId() string`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *GroupSummaryPayload) GetIdOk() (*string, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *GroupSummaryPayload) SetId(v string)`

SetId sets Id field to given value.

### HasId

`func (o *GroupSummaryPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetName

`func (o *GroupSummaryPayload) GetName() string`

GetName returns the Name field if non-nil, zero value otherwise.

### GetNameOk

`func (o *GroupSummaryPayload) GetNameOk() (*string, bool)`

GetNameOk returns a tuple with the Name field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetName

`func (o *GroupSummaryPayload) SetName(v string)`

SetName sets Name field to given value.

### HasName

`func (o *GroupSummaryPayload) HasName() bool`

HasName returns a boolean if a field has been set.

### GetManager

`func (o *GroupSummaryPayload) GetManager() string`

GetManager returns the Manager field if non-nil, zero value otherwise.

### GetManagerOk

`func (o *GroupSummaryPayload) GetManagerOk() (*string, bool)`

GetManagerOk returns a tuple with the Manager field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetManager

`func (o *GroupSummaryPayload) SetManager(v string)`

SetManager sets Manager field to given value.

### HasManager

`func (o *GroupSummaryPayload) HasManager() bool`

HasManager returns a boolean if a field has been set.

### GetIsSystem

`func (o *GroupSummaryPayload) GetIsSystem() bool`

GetIsSystem returns the IsSystem field if non-nil, zero value otherwise.

### GetIsSystemOk

`func (o *GroupSummaryPayload) GetIsSystemOk() (*bool, bool)`

GetIsSystemOk returns a tuple with the IsSystem field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsSystem

`func (o *GroupSummaryPayload) SetIsSystem(v bool)`

SetIsSystem sets IsSystem field to given value.

### HasIsSystem

`func (o *GroupSummaryPayload) HasIsSystem() bool`

HasIsSystem returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


