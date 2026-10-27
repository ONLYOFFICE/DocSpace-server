# FormSubmitDelivery

A delivery whose payload is a FormSubmitPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] 
**payload** | [**FormSubmitPayload**](FormSubmitPayload.md) |  | [optional] 
**webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.form_submit_delivery import FormSubmitDelivery

# TODO update the JSON string below
json = "{}"
# create an instance of FormSubmitDelivery from a JSON string
form_submit_delivery_instance = FormSubmitDelivery.from_json(json)
# print the JSON string representation of the object
print(FormSubmitDelivery.to_json())

# convert the object into a dict
form_submit_delivery_dict = form_submit_delivery_instance.to_dict()
# create an instance of FormSubmitDelivery from a dict
form_submit_delivery_from_dict = FormSubmitDelivery.from_dict(form_submit_delivery_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


