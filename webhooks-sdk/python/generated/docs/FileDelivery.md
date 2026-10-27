# FileDelivery

A delivery whose payload is a FilePayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**FilePayload**](FilePayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.file_delivery import FileDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of FileDelivery from a JSON string
file_delivery_instance = FileDelivery.from_json(json)
# print the JSON string representation of the object
print(FileDelivery.to_json())

# convert the object into a dict
file_delivery_dict = file_delivery_instance.to_dict()
# create an instance of FileDelivery from a dict
file_delivery_from_dict = FileDelivery.from_dict(file_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


