# WebhookEnvelope

The body of every delivery. `payload` is narrowed by `event.trigger` per x-docspace-trigger-payloads. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | **object** | Trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime narrows it.  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.webhook_envelope import WebhookEnvelope

# TODO update the JSON string below
json = "{}"
# create an instance of WebhookEnvelope from a JSON string
webhook_envelope_instance = WebhookEnvelope.from_json(json)
# print the JSON string representation of the object
print(WebhookEnvelope.to_json())

# convert the object into a dict
webhook_envelope_dict = webhook_envelope_instance.to_dict()
# create an instance of WebhookEnvelope from a dict
webhook_envelope_from_dict = WebhookEnvelope.from_dict(webhook_envelope_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


