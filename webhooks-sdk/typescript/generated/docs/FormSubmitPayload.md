
# FormSubmitPayload

Files/Core/Utils/WebhookManager.cs -> SubmittedFormData<T>. The ONLY trigger with a wrapper rather than a bare entry, and the only one whose `webhook.target.id` refers to a different entity (the original form) than the entry that changed. form.filled.out and form.stopped do NOT use this shape -- they send FilePayload. 

## Properties

Name | Type
------------ | -------------
`originalForm` | [FileEntryPayload](FileEntryPayload.md)
`submittedForm` | [FileEntryPayload](FileEntryPayload.md)

## Example

```typescript
import type { FormSubmitPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "originalForm": null,
  "submittedForm": null,
} satisfies FormSubmitPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as FormSubmitPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


