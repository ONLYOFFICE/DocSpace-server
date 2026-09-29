
# RoomPayload

ASC.Files/Core/ApiModels/WebhookDto/RoomWebhookDto.cs. The room.* triggers and every agent.* trigger - an agent is an AI room and has the same shape today.  Not carried: logo, watermark, lifetime, tags, chatSettings. Each needs per-request URL building or an extra query; read them from GET api/2.0/files/rooms/{id}. 

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
`roomType` | number
`type` | number
`filesCount` | number
`foldersCount` | number
`_private` | boolean
`indexing` | boolean
`denyDownload` | boolean
`pinned` | boolean
`quotaLimit` | number
`usedSpace` | number
`color` | string
`cover` | string

## Example

```typescript
import type { RoomPayload } from ''

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
  "roomType": null,
  "type": null,
  "filesCount": null,
  "foldersCount": null,
  "_private": null,
  "indexing": null,
  "denyDownload": null,
  "pinned": null,
  "quotaLimit": null,
  "usedSpace": null,
  "color": null,
  "cover": null,
} satisfies RoomPayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as RoomPayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


