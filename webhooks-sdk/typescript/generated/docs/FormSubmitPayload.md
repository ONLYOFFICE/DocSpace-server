
# FormSubmitPayload

The pair of files carried by a form.submit webhook.  A copy of SubmittedFormData. The only trigger whose payload is a wrapper rather than a bare entry, and the only one whose webhook.target.id refers to a different entity - the original form - than the file that changed. form.filled.out and form.stopped do not use this shape; they carry a plain FileWebhookDto.

## Properties

Name | Type
------------ | -------------
`originalForm` | [FilePayload](FilePayload.md)
`submittedForm` | [FilePayload](FilePayload.md)

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


