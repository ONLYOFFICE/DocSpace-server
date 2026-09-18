
# UserPayload

ASC.Core.Common/Core/UserInfo.cs -- the domain entity, verbatim. It carries NO [JsonIgnore] at all, so every public member below reaches the wire. sid / ssoNameId / ssoSessionId / ldapQouta are null on portals without LDAP or SSO and therefore invisible in most test captures; they DO appear on LDAP/SSO tenants, and are flagged REVIEW below. 

## Properties

Name | Type
------------ | -------------
`id` | string
`firstName` | string
`lastName` | string
`userName` | string
`email` | string
`birthDate` | Date
`sex` | boolean
`status` | number
`activationStatus` | number
`terminatedDate` | Date
`title` | string
`workFromDate` | Date
`location` | string
`notes` | string
`contacts` | string
`contactsList` | Array&lt;string&gt;
`removed` | boolean
`lastModified` | Date
`tenantId` | number
`cultureName` | string
`mobilePhone` | string
`mobilePhoneActivationStatus` | number
`createDate` | Date
`createdBy` | string
`spam` | boolean
`sid` | string
`ldapQouta` | number
`ssoNameId` | string
`ssoSessionId` | string
`isActive` | boolean
`checkActivation` | boolean

## Example

```typescript
import type { UserPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "firstName": null,
  "lastName": null,
  "userName": null,
  "email": null,
  "birthDate": null,
  "sex": null,
  "status": null,
  "activationStatus": null,
  "terminatedDate": null,
  "title": null,
  "workFromDate": null,
  "location": null,
  "notes": null,
  "contacts": null,
  "contactsList": null,
  "removed": null,
  "lastModified": null,
  "tenantId": null,
  "cultureName": null,
  "mobilePhone": null,
  "mobilePhoneActivationStatus": null,
  "createDate": null,
  "createdBy": null,
  "spam": null,
  "sid": null,
  "ldapQouta": null,
  "ssoNameId": null,
  "ssoSessionId": null,
  "isActive": null,
  "checkActivation": null,
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


