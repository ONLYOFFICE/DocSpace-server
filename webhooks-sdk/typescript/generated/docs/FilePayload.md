
# FilePayload

ASC.Files/Core/ApiModels/WebhookDto/FileWebhookDto.cs. A copy of the REST FileDto. Sent by every file.* trigger and by form.filled.out and form.stopped.  Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not - dimensions in particular opens the file stream to measure the image. 

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
`version` | number
`versionGroup` | number
`contentLength` | number
`fileType` | number
`fileExst` | string
`comment` | string
`viewUrl` | string
`webUrl` | string
`encrypted` | boolean
`locked` | boolean
`lockedBy` | string
`isForm` | boolean
`customFilterEnabled` | boolean
`customFilterEnabledBy` | string
`lastOpened` | Date
`vectorizationStatus` | number

## Example

```typescript
import type { FilePayload } from ''

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
  "version": null,
  "versionGroup": null,
  "contentLength": null,
  "fileType": null,
  "fileExst": null,
  "comment": null,
  "viewUrl": null,
  "webUrl": null,
  "encrypted": null,
  "locked": null,
  "lockedBy": null,
  "isForm": null,
  "customFilterEnabled": null,
  "customFilterEnabledBy": null,
  "lastOpened": null,
  "vectorizationStatus": null,
} satisfies FilePayload

console.log(example)

// Convert the instance to a JSON string
const exampleJSON: string = JSON.stringify(example)
console.log(exampleJSON)

// Parse the JSON string back to an object
const exampleParsed = JSON.parse(exampleJSON) as FilePayload
console.log(exampleParsed)
```

[[Back to top]](#) [[Back to API list]](../README.md#api-endpoints) [[Back to Model list]](../README.md#models) [[Back to README]](../README.md)


