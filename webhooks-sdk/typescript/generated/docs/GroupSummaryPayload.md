
# GroupSummaryPayload

A group referenced from another payload.

## Properties

Name | Type
------------ | -------------
`id` | string
`name` | string
`manager` | string
`isSystem` | boolean

## Example

```typescript
import type { GroupSummaryPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "name": null,
  "manager": null,
  "isSystem": null,
} satisfies GroupSummaryPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as GroupSummaryPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


