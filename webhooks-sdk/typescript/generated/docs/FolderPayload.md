
# FolderPayload

ASC.Files/Core/ApiModels/WebhookDto/FolderWebhookDto.cs. The folder.* triggers. Rooms are RoomPayload, never this, even though both come from Folder<T>.  Not carried: inRoom, mute, new - all per-viewer state. 

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
`filesCount` | number
`foldersCount` | number
`type` | number
`isShareable` | boolean

## Example

```typescript
import type { FolderPayload } from ''

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
  "filesCount": null,
  "foldersCount": null,
  "type": null,
  "isShareable": null,
} satisfies FolderPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as FolderPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


