
# WebhookConfigInfo

The subscription, plus its delivery state at send time.

## Properties

Name | Type
------------ | -------------
`id` | number
`name` | string
`url` | string
`triggers` | Array&lt;string&gt;
`target` | [WebhookTargetInfo](WebhookTargetInfo.md)
`lastFailureOn` | Date
`lastFailureContent` | string
`lastSuccessOn` | Date
`retryCount` | number
`retryOn` | Date

## Example

```typescript
import type { WebhookConfigInfo } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "name": null,
  "url": null,
  "triggers": null,
  "target": null,
  "lastFailureOn": null,
  "lastFailureContent": null,
  "lastSuccessOn": null,
  "retryCount": null,
  "retryOn": null,
} satisfies WebhookConfigInfo

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as WebhookConfigInfo
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


