# DocSpace.Webhooks.SDK.Model.FormSubmitPayload
The pair of files carried by a form.submit webhook.  A copy of SubmittedFormData. The only trigger whose payload is a wrapper rather than a bare entry, and the only one whose webhook.target.id refers to a different entity - the original form - than the file that changed. form.filled.out and form.stopped do not use this shape; they carry a plain FileWebhookDto.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**OriginalForm** | [**FilePayload**](FilePayload.md) |  | [optional] 
**SubmittedForm** | [**FilePayload**](FilePayload.md) |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

