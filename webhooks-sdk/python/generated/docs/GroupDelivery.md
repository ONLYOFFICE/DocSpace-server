# GroupDelivery

A delivery whose payload is a GroupPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**GroupPayload**](GroupPayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.group_delivery import GroupDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of GroupDelivery from a JSON string
group_delivery_instance = GroupDelivery.from_json(json)
# print the JSON string representation of the object
print(GroupDelivery.to_json())

# convert the object into a dict
group_delivery_dict = group_delivery_instance.to_dict()
# create an instance of GroupDelivery from a dict
group_delivery_from_dict = GroupDelivery.from_dict(group_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


