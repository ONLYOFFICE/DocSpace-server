
# GroupPayload

ASC.Core.Common/Core/GroupInfo.cs. Note `ID` in C#; the camelCase policy lowercases the whole leading run, so it is \"id\" on the wire, while `CategoryID` becomes \"categoryID\". 

## Properties

Name | Type
------------ | -------------
`id` | string
`name` | string
`categoryID` | string
`parent` | [GroupPayload](GroupPayload.md)
`sid` | string
`removed` | boolean

## Example

```typescript
import type { GroupPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "name": null,
  "categoryID": null,
  "parent": null,
  "sid": null,
  "removed": null,
} satisfies GroupPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as GroupPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


