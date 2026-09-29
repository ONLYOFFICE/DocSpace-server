

# FormSubmitPayload

ASC.Files/Core/ApiModels/WebhookDto/FormSubmitWebhookDto.cs. The ONLY trigger with a wrapper rather than a bare entry, and the only one whose `webhook.target.id` refers to a different entity (the original form) than the entry that changed. form.filled.out and form.stopped do NOT use this shape -- they send FilePayload. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**originalForm** | [**FilePayload**](FilePayload.md) |  |  [optional] |
|**submittedForm** | [**FilePayload**](FilePayload.md) |  |  [optional] |



