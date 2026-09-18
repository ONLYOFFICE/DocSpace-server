# FormSubmitPayload

Files/Core/Utils/WebhookManager.cs -> SubmittedFormData<T>. The ONLY trigger with a wrapper rather than a bare entry, and the only one whose `webhook.target.id` refers to a different entity (the original form) than the entry that changed. form.filled.out and form.stopped do NOT use this shape -- they send FilePayload. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**original_form** | [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] 
**submitted_form** | [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.form_submit_payload import FormSubmitPayload

# TODO update the JSON string below
json = "{}"
# create an instance of FormSubmitPayload from a JSON string
form_submit_payload_instance = FormSubmitPayload.from_json(json)
# print the JSON string representation of the object
print(FormSubmitPayload.to_json())

# convert the object into a dict
form_submit_payload_dict = form_submit_payload_instance.to_dict()
# create an instance of FormSubmitPayload from a dict
form_submit_payload_from_dict = FormSubmitPayload.from_dict(form_submit_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


