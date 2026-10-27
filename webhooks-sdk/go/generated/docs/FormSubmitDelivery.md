# FormSubmitDelivery

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Event** | Pointer to [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**Payload** | Pointer to [**FormSubmitPayload**](FormSubmitPayload.md) |  | [optional] 
**Webhook** | Pointer to [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Methods

### NewFormSubmitDelivery

`func NewFormSubmitDelivery() *FormSubmitDelivery`

NewFormSubmitDelivery instantiates a new FormSubmitDelivery object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFormSubmitDeliveryWithDefaults

`func NewFormSubmitDeliveryWithDefaults() *FormSubmitDelivery`

NewFormSubmitDeliveryWithDefaults instantiates a new FormSubmitDelivery object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetEvent

`func (o *FormSubmitDelivery) GetEvent() WebhookEventInfo`

GetEvent returns the Event field if non-nil, zero value otherwise.

### GetEventOk

`func (o *FormSubmitDelivery) GetEventOk() (*WebhookEventInfo, bool)`

GetEventOk returns a tuple with the Event field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEvent

`func (o *FormSubmitDelivery) SetEvent(v WebhookEventInfo)`

SetEvent sets Event field to given value.

### HasEvent

`func (o *FormSubmitDelivery) HasEvent() bool`

HasEvent returns a boolean if a field has been set.

### GetPayload

`func (o *FormSubmitDelivery) GetPayload() FormSubmitPayload`

GetPayload returns the Payload field if non-nil, zero value otherwise.

### GetPayloadOk

`func (o *FormSubmitDelivery) GetPayloadOk() (*FormSubmitPayload, bool)`

GetPayloadOk returns a tuple with the Payload field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPayload

`func (o *FormSubmitDelivery) SetPayload(v FormSubmitPayload)`

SetPayload sets Payload field to given value.

### HasPayload

`func (o *FormSubmitDelivery) HasPayload() bool`

HasPayload returns a boolean if a field has been set.

### GetWebhook

`func (o *FormSubmitDelivery) GetWebhook() WebhookConfigInfo`

GetWebhook returns the Webhook field if non-nil, zero value otherwise.

### GetWebhookOk

`func (o *FormSubmitDelivery) GetWebhookOk() (*WebhookConfigInfo, bool)`

GetWebhookOk returns a tuple with the Webhook field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWebhook

`func (o *FormSubmitDelivery) SetWebhook(v WebhookConfigInfo)`

SetWebhook sets Webhook field to given value.

### HasWebhook

`func (o *FormSubmitDelivery) HasWebhook() bool`

HasWebhook returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


