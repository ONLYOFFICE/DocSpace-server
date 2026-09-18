# WebhookEventInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **int32** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  | [optional] 
**CreateOn** | Pointer to **time.Time** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  | [optional] 
**CreateBy** | Pointer to **string** | Acting user. Omitted when the empty guid (background jobs). | [optional] 
**Trigger** | Pointer to **string** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] 
**TriggerId** | Pointer to **int64** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional] 

## Methods

### NewWebhookEventInfo

`func NewWebhookEventInfo() *WebhookEventInfo`

NewWebhookEventInfo instantiates a new WebhookEventInfo object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewWebhookEventInfoWithDefaults

`func NewWebhookEventInfoWithDefaults() *WebhookEventInfo`

NewWebhookEventInfoWithDefaults instantiates a new WebhookEventInfo object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *WebhookEventInfo) GetId() int32`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *WebhookEventInfo) GetIdOk() (*int32, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *WebhookEventInfo) SetId(v int32)`

SetId sets Id field to given value.

### HasId

`func (o *WebhookEventInfo) HasId() bool`

HasId returns a boolean if a field has been set.

### GetCreateOn

`func (o *WebhookEventInfo) GetCreateOn() time.Time`

GetCreateOn returns the CreateOn field if non-nil, zero value otherwise.

### GetCreateOnOk

`func (o *WebhookEventInfo) GetCreateOnOk() (*time.Time, bool)`

GetCreateOnOk returns a tuple with the CreateOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreateOn

`func (o *WebhookEventInfo) SetCreateOn(v time.Time)`

SetCreateOn sets CreateOn field to given value.

### HasCreateOn

`func (o *WebhookEventInfo) HasCreateOn() bool`

HasCreateOn returns a boolean if a field has been set.

### GetCreateBy

`func (o *WebhookEventInfo) GetCreateBy() string`

GetCreateBy returns the CreateBy field if non-nil, zero value otherwise.

### GetCreateByOk

`func (o *WebhookEventInfo) GetCreateByOk() (*string, bool)`

GetCreateByOk returns a tuple with the CreateBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreateBy

`func (o *WebhookEventInfo) SetCreateBy(v string)`

SetCreateBy sets CreateBy field to given value.

### HasCreateBy

`func (o *WebhookEventInfo) HasCreateBy() bool`

HasCreateBy returns a boolean if a field has been set.

### GetTrigger

`func (o *WebhookEventInfo) GetTrigger() string`

GetTrigger returns the Trigger field if non-nil, zero value otherwise.

### GetTriggerOk

`func (o *WebhookEventInfo) GetTriggerOk() (*string, bool)`

GetTriggerOk returns a tuple with the Trigger field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTrigger

`func (o *WebhookEventInfo) SetTrigger(v string)`

SetTrigger sets Trigger field to given value.

### HasTrigger

`func (o *WebhookEventInfo) HasTrigger() bool`

HasTrigger returns a boolean if a field has been set.

### GetTriggerId

`func (o *WebhookEventInfo) GetTriggerId() int64`

GetTriggerId returns the TriggerId field if non-nil, zero value otherwise.

### GetTriggerIdOk

`func (o *WebhookEventInfo) GetTriggerIdOk() (*int64, bool)`

GetTriggerIdOk returns a tuple with the TriggerId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTriggerId

`func (o *WebhookEventInfo) SetTriggerId(v int64)`

SetTriggerId sets TriggerId field to given value.

### HasTriggerId

`func (o *WebhookEventInfo) HasTriggerId() bool`

HasTriggerId returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


