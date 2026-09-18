
# WebhookEventInfo


## Properties

Name | Type
------------ | -------------
`id` | number
`createOn` | Date
`createBy` | string
`trigger` | string
`triggerId` | number

## Example

```typescript
import type { WebhookEventInfo } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "createOn": null,
  "createBy": null,
  "trigger": null,
  "triggerId": null,
} satisfies WebhookEventInfo

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as WebhookEventInfo
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


