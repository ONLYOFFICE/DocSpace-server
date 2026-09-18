# WebhookEventInfo


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  | [optional] 
**create_on** | **datetime** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  | [optional] 
**create_by** | **UUID** | Acting user. Omitted when the empty guid (background jobs). | [optional] 
**trigger** | **str** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] 
**trigger_id** | **int** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.webhook_event_info import WebhookEventInfo

# TODO update the JSON string below
json = "{}"
# create an instance of WebhookEventInfo from a JSON string
webhook_event_info_instance = WebhookEventInfo.from_json(json)
# print the JSON string representation of the object
print(WebhookEventInfo.to_json())

# convert the object into a dict
webhook_event_info_dict = webhook_event_info_instance.to_dict()
# create an instance of WebhookEventInfo from a dict
webhook_event_info_from_dict = WebhookEventInfo.from_dict(webhook_event_info_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


