# WebhookConfigInfo

The subscription, plus its delivery state at send time.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** | The subscription ID. | [optional] 
**name** | **str** | The subscription name given by its creator. | [optional] 
**url** | **str** | The URL the delivery is posted to. | [optional] 
**triggers** | **List[str]** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**last_failure_on** | **datetime** | When the last delivery to this subscription failed, in UTC; absent when none has failed. | [optional] 
**last_failure_content** | **str** | What the receiver answered, or the network error, when the last delivery to this subscription failed. | [optional] 
**last_success_on** | **datetime** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. | [optional] 
**retry_count** | **int** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. | [optional] 
**retry_on** | **datetime** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.webhook_config_info import WebhookConfigInfo

# TODO update the JSON string below
json = "{}"
# create an instance of WebhookConfigInfo from a JSON string
webhook_config_info_instance = WebhookConfigInfo.from_json(json)
# print the JSON string representation of the object
print(WebhookConfigInfo.to_json())

# convert the object into a dict
webhook_config_info_dict = webhook_config_info_instance.to_dict()
# create an instance of WebhookConfigInfo from a dict
webhook_config_info_from_dict = WebhookConfigInfo.from_dict(webhook_config_info_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


