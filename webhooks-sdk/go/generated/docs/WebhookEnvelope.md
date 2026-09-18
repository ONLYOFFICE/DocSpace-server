# WebhookEnvelope

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Event** | Pointer to [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**Payload** | Pointer to **interface{}** | Trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime narrows it.  | [optional] 
**Webhook** | Pointer to [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Methods

### NewWebhookEnvelope

`func NewWebhookEnvelope() *WebhookEnvelope`

NewWebhookEnvelope instantiates a new WebhookEnvelope object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewWebhookEnvelopeWithDefaults

`func NewWebhookEnvelopeWithDefaults() *WebhookEnvelope`

NewWebhookEnvelopeWithDefaults instantiates a new WebhookEnvelope object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetEvent

`func (o *WebhookEnvelope) GetEvent() WebhookEventInfo`

GetEvent returns the Event field if non-nil, zero value otherwise.

### GetEventOk

`func (o *WebhookEnvelope) GetEventOk() (*WebhookEventInfo, bool)`

GetEventOk returns a tuple with the Event field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEvent

`func (o *WebhookEnvelope) SetEvent(v WebhookEventInfo)`

SetEvent sets Event field to given value.

### HasEvent

`func (o *WebhookEnvelope) HasEvent() bool`

HasEvent returns a boolean if a field has been set.

### GetPayload

`func (o *WebhookEnvelope) GetPayload() interface{}`

GetPayload returns the Payload field if non-nil, zero value otherwise.

### GetPayloadOk

`func (o *WebhookEnvelope) GetPayloadOk() (*interface{}, bool)`

GetPayloadOk returns a tuple with the Payload field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPayload

`func (o *WebhookEnvelope) SetPayload(v interface{})`

SetPayload sets Payload field to given value.

### HasPayload

`func (o *WebhookEnvelope) HasPayload() bool`

HasPayload returns a boolean if a field has been set.

### SetPayloadNil

`func (o *WebhookEnvelope) SetPayloadNil(b bool)`

 SetPayloadNil sets the value for Payload to be an explicit nil

### UnsetPayload
`func (o *WebhookEnvelope) UnsetPayload()`

UnsetPayload ensures that no value is present for Payload, not even an explicit nil
### GetWebhook

`func (o *WebhookEnvelope) GetWebhook() WebhookConfigInfo`

GetWebhook returns the Webhook field if non-nil, zero value otherwise.

### GetWebhookOk

`func (o *WebhookEnvelope) GetWebhookOk() (*WebhookConfigInfo, bool)`

GetWebhookOk returns a tuple with the Webhook field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWebhook

`func (o *WebhookEnvelope) SetWebhook(v WebhookConfigInfo)`

SetWebhook sets Webhook field to given value.

### HasWebhook

`func (o *WebhookEnvelope) HasWebhook() bool`

HasWebhook returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


