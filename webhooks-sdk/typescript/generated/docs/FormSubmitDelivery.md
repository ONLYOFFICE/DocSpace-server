
# FormSubmitDelivery

A delivery whose payload is a FormSubmitPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type
------------ | -------------
`event` | [WebhookEventInfo](WebhookEventInfo.md)
`payload` | [FormSubmitPayload](FormSubmitPayload.md)
`webhook` | [WebhookConfigInfo](WebhookConfigInfo.md)

## Example

```typescript
import type { FormSubmitDelivery } from ''

// TODO: Update the object below with actual values
const example = {
  "event": null,
  "payload": null,
  "webhook": null,
} satisfies FormSubmitDelivery

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as FormSubmitDelivery
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


