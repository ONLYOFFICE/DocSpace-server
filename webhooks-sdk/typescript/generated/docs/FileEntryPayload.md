
# FileEntryPayload

What every file, folder and room webhook payload has in common.  A copy of FileEntryDto, deliberately not derived from it, and populated straight from the domain entity rather than through FileEntryDtoHelper. Three groups of the REST fields are not carried:  - caller-relative state - access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. A delivery has no caller; who may receive it is already decided by WebhookFileEntryAccessChecker against the subscription owner, and putting one user\'s permission matrix on the wire would be both meaningless and a disclosure.  - anything the REST helper pays for per request - thumbnails, image dimensions (it opens the file stream), form-filling roles, view accessibility, external share links.  - subtype fields belong on the subtype, unlike the previous payload where File and Folder members were erased by the declared parameter type and never reached the wire at all.  Timestamps are UTC, matching the delivery envelope, rather than the tenant-local ApiDateTime.

## Properties

Name | Type
------------ | -------------
`id` | [EntryId](EntryId.md)
`parentId` | [EntryId](EntryId.md)
`rootFolderId` | [EntryId](EntryId.md)
`title` | string
`fileEntryType` | number
`created` | Date
`createdBy` | [UserSummaryPayload](UserSummaryPayload.md)
`updated` | Date
`updatedBy` | [UserSummaryPayload](UserSummaryPayload.md)
`rootFolderType` | number
`parentRoomType` | number
`originId` | [EntryId](EntryId.md)
`originRoomId` | [EntryId](EntryId.md)
`originTitle` | string
`originRoomTitle` | string
`providerItem` | boolean
`providerKey` | string
`providerId` | number
`order` | number

## Example

```typescript
import type { FileEntryPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "parentId": null,
  "rootFolderId": null,
  "title": null,
  "fileEntryType": null,
  "created": null,
  "createdBy": null,
  "updated": null,
  "updatedBy": null,
  "rootFolderType": null,
  "parentRoomType": null,
  "originId": null,
  "originRoomId": null,
  "originTitle": null,
  "originRoomTitle": null,
  "providerItem": null,
  "providerKey": null,
  "providerId": null,
  "order": null,
} satisfies FileEntryPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as FileEntryPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


