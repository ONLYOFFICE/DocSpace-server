
# WebhookEnvelope

The body of every delivery. `payload` is narrowed by `event.trigger` per x-docspace-trigger-payloads. 

## Properties

Name | Type
------------ | -------------
`event` | [WebhookEventInfo](WebhookEventInfo.md)
`payload` | any
`webhook` | [WebhookConfigInfo](WebhookConfigInfo.md)

## Example

```typescript
import type { WebhookEnvelope } from ''

// TODO: Update the object below with actual values
const example = {
  "event": null,
  "payload": null,
  "webhook": null,
} satisfies WebhookEnvelope

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as WebhookEnvelope
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


