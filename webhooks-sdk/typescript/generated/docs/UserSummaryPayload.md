
# UserSummaryPayload

A user referenced from another payload.

## Properties

Name | Type
------------ | -------------
`id` | string
`displayName` | string
`userName` | string
`email` | string
`avatar` | string
`profileUrl` | string

## Example

```typescript
import type { UserSummaryPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "displayName": null,
  "userName": null,
  "email": null,
  "avatar": null,
  "profileUrl": null,
} satisfies UserSummaryPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as UserSummaryPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


