# WebhookEventInfo

The event a delivery reports.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | The ID of the first delivery-log record of the event, and the idempotency key. It is stable across every resend of one event, the server&#39;s automatic retries and a manual retry from the admin UI alike: a manual retry is a delivery-log record of its own, but it still sends the ID of the first one. The same ID therefore always means the same logical event. It is also echoed in the x-docspace-event-id header. | [optional] 
**create_on** | **datetime** | When the event happened, in UTC and whole seconds. It stays the same on every retry, automatic or manual, so it says nothing about when the request was sent: a freshness check on it would reject legitimate retries, and the replay defence is deduplication on &#x60;id&#x60;. Echoed in the unsigned x-docspace-event-timestamp header. | [optional] 
**create_by** | **UUID** | The ID of the user whose action caused the event. Omitted when it is the empty GUID, as for background jobs. | [optional] 
**trigger** | **str** | The trigger name, e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] 
**trigger_id** | **int** | The bit value of the trigger. Omitted when 0, the \&quot;*\&quot; fallback. | [optional] 

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


