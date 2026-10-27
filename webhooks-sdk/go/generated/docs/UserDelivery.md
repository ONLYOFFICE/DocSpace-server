# UserDelivery

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Event** | Pointer to [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**Payload** | Pointer to [**UserPayload**](UserPayload.md) |  | [optional] 
**Webhook** | Pointer to [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Methods

### NewUserDelivery

`func NewUserDelivery() *UserDelivery`

NewUserDelivery instantiates a new UserDelivery object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewUserDeliveryWithDefaults

`func NewUserDeliveryWithDefaults() *UserDelivery`

NewUserDeliveryWithDefaults instantiates a new UserDelivery object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetEvent

`func (o *UserDelivery) GetEvent() WebhookEventInfo`

GetEvent returns the Event field if non-nil, zero value otherwise.

### GetEventOk

`func (o *UserDelivery) GetEventOk() (*WebhookEventInfo, bool)`

GetEventOk returns a tuple with the Event field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEvent

`func (o *UserDelivery) SetEvent(v WebhookEventInfo)`

SetEvent sets Event field to given value.

### HasEvent

`func (o *UserDelivery) HasEvent() bool`

HasEvent returns a boolean if a field has been set.

### GetPayload

`func (o *UserDelivery) GetPayload() UserPayload`

GetPayload returns the Payload field if non-nil, zero value otherwise.

### GetPayloadOk

`func (o *UserDelivery) GetPayloadOk() (*UserPayload, bool)`

GetPayloadOk returns a tuple with the Payload field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPayload

`func (o *UserDelivery) SetPayload(v UserPayload)`

SetPayload sets Payload field to given value.

### HasPayload

`func (o *UserDelivery) HasPayload() bool`

HasPayload returns a boolean if a field has been set.

### GetWebhook

`func (o *UserDelivery) GetWebhook() WebhookConfigInfo`

GetWebhook returns the Webhook field if non-nil, zero value otherwise.

### GetWebhookOk

`func (o *UserDelivery) GetWebhookOk() (*WebhookConfigInfo, bool)`

GetWebhookOk returns a tuple with the Webhook field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWebhook

`func (o *UserDelivery) SetWebhook(v WebhookConfigInfo)`

SetWebhook sets Webhook field to given value.

### HasWebhook

`func (o *UserDelivery) HasWebhook() bool`

HasWebhook returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


