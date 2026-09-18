# WebhookConfigInfo

The subscription, plus its delivery state at send time.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **int** |  | [optional] 
**name** | **str** |  | [optional] 
**url** | **str** |  | [optional] 
**triggers** | **List[str]** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**last_failure_on** | **datetime** |  | [optional] 
**last_failure_content** | **str** |  | [optional] 
**last_success_on** | **datetime** |  | [optional] 
**retry_count** | **int** | Omitted on the first attempt (0). Present from attempt 2. | [optional] 
**retry_on** | **datetime** |  | [optional] 

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


