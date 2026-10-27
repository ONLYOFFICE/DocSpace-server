# FolderDelivery

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Event** | Pointer to [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**Payload** | Pointer to [**FolderPayload**](FolderPayload.md) |  | [optional] 
**Webhook** | Pointer to [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Methods

### NewFolderDelivery

`func NewFolderDelivery() *FolderDelivery`

NewFolderDelivery instantiates a new FolderDelivery object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFolderDeliveryWithDefaults

`func NewFolderDeliveryWithDefaults() *FolderDelivery`

NewFolderDeliveryWithDefaults instantiates a new FolderDelivery object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetEvent

`func (o *FolderDelivery) GetEvent() WebhookEventInfo`

GetEvent returns the Event field if non-nil, zero value otherwise.

### GetEventOk

`func (o *FolderDelivery) GetEventOk() (*WebhookEventInfo, bool)`

GetEventOk returns a tuple with the Event field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEvent

`func (o *FolderDelivery) SetEvent(v WebhookEventInfo)`

SetEvent sets Event field to given value.

### HasEvent

`func (o *FolderDelivery) HasEvent() bool`

HasEvent returns a boolean if a field has been set.

### GetPayload

`func (o *FolderDelivery) GetPayload() FolderPayload`

GetPayload returns the Payload field if non-nil, zero value otherwise.

### GetPayloadOk

`func (o *FolderDelivery) GetPayloadOk() (*FolderPayload, bool)`

GetPayloadOk returns a tuple with the Payload field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetPayload

`func (o *FolderDelivery) SetPayload(v FolderPayload)`

SetPayload sets Payload field to given value.

### HasPayload

`func (o *FolderDelivery) HasPayload() bool`

HasPayload returns a boolean if a field has been set.

### GetWebhook

`func (o *FolderDelivery) GetWebhook() WebhookConfigInfo`

GetWebhook returns the Webhook field if non-nil, zero value otherwise.

### GetWebhookOk

`func (o *FolderDelivery) GetWebhookOk() (*WebhookConfigInfo, bool)`

GetWebhookOk returns a tuple with the Webhook field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWebhook

`func (o *FolderDelivery) SetWebhook(v WebhookConfigInfo)`

SetWebhook sets Webhook field to given value.

### HasWebhook

`func (o *FolderDelivery) HasWebhook() bool`

HasWebhook returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


