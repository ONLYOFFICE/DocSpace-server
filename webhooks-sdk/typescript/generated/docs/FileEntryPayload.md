
# FileEntryPayload

The payload for EVERY file, folder, room, agent and form trigger.  There is deliberately no File- or Folder-specific schema. WebhookManager calls PublishAsync<T1,T2> with a static parameter type of FileEntry<T>, so T1 binds to the abstract base and System.Text.Json serializes by DECLARED type. File<T> and Folder<T> members -- pureTitle, version, contentLength, folderType, filesCount, isRoom -- therefore never reach the wire, however the entry was published.  One consequence worth internalising: `title` IS present, even for files. File<T> hides Title behind [JsonIgnore] and exposes pureTitle instead, but that override is invisible here because the base declaration is what gets serialized.  Verified against a captured production delivery: all 14 keys of a real file.created payload are members of this schema and nothing else. (Files/Core/Core/Entries/FileEntry.cs; [JsonIgnore] members excluded.) 

## Properties

Name | Type
------------ | -------------
`id` | [EntryId](EntryId.md)
`parentId` | [EntryId](EntryId.md)
`rootId` | [EntryId](EntryId.md)
`originId` | [EntryId](EntryId.md)
`originRoomId` | [EntryId](EntryId.md)
`folderIdDisplay` | [EntryId](EntryId.md)
`mutableId` | boolean
`title` | string
`isNew` | boolean
`createBy` | string
`createOn` | Date
`modifiedBy` | string
`modifiedOn` | Date
`sharedBy` | string
`rootCreateBy` | string
`parentRoomCreatedBy` | string
`rootFolderType` | number
`parentRoomType` | number
`fileEntryType` | number
`access` | number
`shared` | boolean
`sharedForUser` | boolean
`sharedExternal` | boolean
`parentShared` | boolean
`providerId` | number
`providerKey` | string
`originTitle` | string
`originRoomTitle` | string
`order` | number
`error` | string
`tags` | Array&lt;{ [key: string]: any; }&gt;
`shareRecord` | { [key: string]: any; }
`security` | { [key: string]: boolean; }
`securityByUsers` | { [key: string]: { [key: string]: boolean; }; }

## Example

```typescript
import type { FileEntryPayload } from ''

// TODO: Update the object below with actual values
const example = {
  "id": null,
  "parentId": null,
  "rootId": null,
  "originId": null,
  "originRoomId": null,
  "folderIdDisplay": null,
  "mutableId": null,
  "title": null,
  "isNew": null,
  "createBy": null,
  "createOn": null,
  "modifiedBy": null,
  "modifiedOn": null,
  "sharedBy": null,
  "rootCreateBy": null,
  "parentRoomCreatedBy": null,
  "rootFolderType": null,
  "parentRoomType": null,
  "fileEntryType": null,
  "access": null,
  "shared": null,
  "sharedForUser": null,
  "sharedExternal": null,
  "parentShared": null,
  "providerId": null,
  "providerKey": null,
  "originTitle": null,
  "originRoomTitle": null,
  "order": null,
  "error": null,
  "tags": null,
  "shareRecord": null,
  "security": null,
  "securityByUsers": null,
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


