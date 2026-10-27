
# RoomDelivery

A delivery whose payload is a RoomPayload: WebhookEnvelope with `payload` typed.

## Properties

Name | Type
------------ | -------------
`event` | [WebhookEventInfo](WebhookEventInfo.md)
`payload` | [RoomPayload](RoomPayload.md)
`webhook` | [WebhookConfigInfo](WebhookConfigInfo.md)

## Example

```typescript
import type { RoomDelivery } from ''

// TODO: Update the object below with actual values
const example = {
  "event": null,
  "payload": null,
  "webhook": null,
} satisfies RoomDelivery

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as RoomDelivery
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


