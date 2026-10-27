# RoomDelivery

A delivery whose payload is a RoomPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**RoomPayload**](RoomPayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.room_delivery import RoomDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of RoomDelivery from a JSON string
room_delivery_instance = RoomDelivery.from_json(json)
# print the JSON string representation of the object
print(RoomDelivery.to_json())

# convert the object into a dict
room_delivery_dict = room_delivery_instance.to_dict()
# create an instance of RoomDelivery from a dict
room_delivery_from_dict = RoomDelivery.from_dict(room_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


