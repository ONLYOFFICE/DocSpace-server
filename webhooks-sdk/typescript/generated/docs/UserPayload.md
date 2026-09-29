
# UserPayload

ASC.Api.Core/Webhook/Payloads/UserWebhookDto.cs. A copy of the REST EmployeeFullDto, owned by the webhook contract and free to diverge from it.  Not carried, deliberately: sid / ssoNameId / ssoSessionId (LDAP and SAML identifiers, and a SAML SESSION id, all of which the domain entity used to put on the wire), loginEventId, authCookieLifetime, tfaAppEnabled, theme, isAnonim, and `shared`. `contacts` appears once, as a typed list, rather than twice in two shapes. 

## Properties

Name | Type
------------ | -------------
`id` | string
`displayName` | string
`firstName` | string
`lastName` | string
`userName` | string
`email` | string
`contacts` | [Array&lt;ContactPayload&gt;](ContactPayload.md)
`status` | number
`activationStatus` | number
`terminated` | Date
`department` | string
`groups` | [Array&lt;GroupSummaryPayload&gt;](GroupSummaryPayload.md)
`location` | string
`notes` | string
`isAdmin` | boolean
`isRoomAdmin` | boolean
`isOwner` | boolean
`isVisitor` | boolean
`isCollaborator` | boolean
`isLDAP` | boolean
`isSSO` | boolean
`listAdminModules` | Array&lt;string&gt;
`cultureName` | string
`mobilePhone` | string
`mobilePhoneActivationStatus` | number
`quotaLimit` | number
`usedSpace` | number
`isCustomQuota` | boolean
`createdBy` | [UserSummaryPayload](UserSummaryPayload.md)
`registrationDate` | Date
`hasAvatar` | boolean
`avatar` | string
`avatarOriginal` | string
`avatarMax` | string
`avatarMedium` | string
`avatarSmall` | string
`profileUrl` | string

## Example

```typescript
import type { UserPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "displayName": null,
  "firstName": null,
  "lastName": null,
  "userName": null,
  "email": null,
  "contacts": null,
  "status": null,
  "activationStatus": null,
  "terminated": null,
  "department": null,
  "groups": null,
  "location": null,
  "notes": null,
  "isAdmin": null,
  "isRoomAdmin": null,
  "isOwner": null,
  "isVisitor": null,
  "isCollaborator": null,
  "isLDAP": null,
  "isSSO": null,
  "listAdminModules": null,
  "cultureName": null,
  "mobilePhone": null,
  "mobilePhoneActivationStatus": null,
  "quotaLimit": null,
  "usedSpace": null,
  "isCustomQuota": null,
  "createdBy": null,
  "registrationDate": null,
  "hasAvatar": null,
  "avatar": null,
  "avatarOriginal": null,
  "avatarMax": null,
  "avatarMedium": null,
  "avatarSmall": null,
  "profileUrl": null,
} satisfies UserPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as UserPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


