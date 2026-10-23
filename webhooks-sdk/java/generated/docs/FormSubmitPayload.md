

# FormSubmitPayload

The pair of files carried by a form.submit webhook.  A copy of SubmittedFormData. The only trigger whose payload is a wrapper rather than a bare entry, and the only one whose webhook.target.id refers to a different entity - the original form - than the file that changed. form.filled.out and form.stopped do not use this shape; they carry a plain FileWebhookDto.

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**originalForm** | [**FilePayload**](FilePayload.md) |  |  [optional] |
|**submittedForm** | [**FilePayload**](FilePayload.md) |  |  [optional] |



