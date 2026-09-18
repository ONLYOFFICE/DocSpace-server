# WebhookTargetInfo

Present only when the subscription is scoped to one entity.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**type** | **str** |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.webhook_target_info import WebhookTargetInfo

# TODO update the JSON string below
json = "{}"
# create an instance of WebhookTargetInfo from a JSON string
webhook_target_info_instance = WebhookTargetInfo.from_json(json)
# print the JSON string representation of the object
print(WebhookTargetInfo.to_json())

# convert the object into a dict
webhook_target_info_dict = webhook_target_info_instance.to_dict()
# create an instance of WebhookTargetInfo from a dict
webhook_target_info_from_dict = WebhookTargetInfo.from_dict(webhook_target_info_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


