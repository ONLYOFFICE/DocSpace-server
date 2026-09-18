

# FormSubmitPayload

Files/Core/Utils/WebhookManager.cs -> SubmittedFormData<T>. The ONLY trigger with a wrapper rather than a bare entry, and the only one whose `webhook.target.id` refers to a different entity (the original form) than the entry that changed. form.filled.out and form.stopped do NOT use this shape -- they send FilePayload. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**originalForm** | [**FileEntryPayload**](FileEntryPayload.md) |  |  [optional] |
|**submittedForm** | [**FileEntryPayload**](FileEntryPayload.md) |  |  [optional] |



