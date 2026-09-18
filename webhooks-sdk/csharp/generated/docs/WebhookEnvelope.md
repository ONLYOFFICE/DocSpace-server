# DocSpace.Webhooks.SDK.Model.WebhookEnvelope
The body of every delivery. `payload` is narrowed by `event.trigger` per x-docspace-trigger-payloads. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**Payload** | **Object** | Trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime narrows it.  | [optional] 
**Webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

