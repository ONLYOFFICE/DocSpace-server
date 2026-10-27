# UserDelivery

A delivery whose payload is a UserPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**UserPayload**](UserPayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.user_delivery import UserDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of UserDelivery from a JSON string
user_delivery_instance = UserDelivery.from_json(json)
# print the JSON string representation of the object
print(UserDelivery.to_json())

# convert the object into a dict
user_delivery_dict = user_delivery_instance.to_dict()
# create an instance of UserDelivery from a dict
user_delivery_from_dict = UserDelivery.from_dict(user_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


