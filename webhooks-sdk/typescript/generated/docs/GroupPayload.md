
# GroupPayload

ASC.Api.Core/Webhook/Payloads/GroupWebhookDto.cs. A copy of the REST GroupDto.  The member list is not carried - a group can hold thousands of users and each would be expanded into every group event. `membersCount` is the hint that the roster changed; read it from GET api/2.0/group/{id}. 

## Properties

Name | Type
------------ | -------------
`id` | string
`name` | string
`parent` | string
`category` | string
`isLDAP` | boolean
`isSystem` | boolean
`manager` | [UserSummaryPayload](UserSummaryPayload.md)
`membersCount` | number

## Example

```typescript
import type { GroupPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "name": null,
  "parent": null,
  "category": null,
  "isLDAP": null,
  "isSystem": null,
  "manager": null,
  "membersCount": null,
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


