
# FileEntryPayload

ASC.Files/Core/ApiModels/WebhookDto/FileEntryWebhookDto.cs. What FilePayload, FolderPayload and RoomPayload have in common.  This schema is never sent on its own -- unlike the previous contract, where every file, folder, room, agent and form trigger sent exactly this and nothing else. The old payload was typed as the abstract FileEntry<T> at the publish site, so System.Text.Json serialized by DECLARED type and every File<T> and Folder<T> member was silently dropped: version, contentLength, fileType, folderType, filesCount, roomType never reached a receiver. That is fixed; the subtypes below carry their own fields.  Not carried, deliberately: access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. Those answer \"what may the caller see\", and a delivery has no caller - who receives it is decided by WebhookFileEntryAccessChecker against the subscription owner. Putting one user\'s permission matrix on the wire was both meaningless to the receiver and a disclosure. 

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


