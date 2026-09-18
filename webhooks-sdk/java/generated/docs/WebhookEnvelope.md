

# WebhookEnvelope

The body of every delivery. `payload` is narrowed by `event.trigger` per x-docspace-trigger-payloads. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  |  [optional] |
|**payload** | **Object** | Trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime narrows it.  |  [optional] |
|**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  |  [optional] |



