# FolderDelivery

A delivery whose payload is a FolderPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**FolderPayload**](FolderPayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.folder_delivery import FolderDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of FolderDelivery from a JSON string
folder_delivery_instance = FolderDelivery.from_json(json)
# print the JSON string representation of the object
print(FolderDelivery.to_json())

# convert the object into a dict
folder_delivery_dict = folder_delivery_instance.to_dict()
# create an instance of FolderDelivery from a dict
folder_delivery_from_dict = FolderDelivery.from_dict(folder_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


