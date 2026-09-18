
# WebhookTargetInfo

Present only when the subscription is scoped to one entity.

## Properties

Name | Type
------------ | -------------
`id` | [EntryId](EntryId.md)
`type` | string

## Example

```typescript
import type { WebhookTargetInfo } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "type": null,
} satisfies WebhookTargetInfo

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as WebhookTargetInfo
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


