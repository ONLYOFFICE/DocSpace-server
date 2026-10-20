# Change Log

## 4.0.0

### Added

- Added the reworked AI API surface, 95 requests in new folders under `AI`: `AI` (run an action, stream it, approve or deny a tool call), `Assignments`, `Attachments`, `Context`, `Editor tools`, `Export`, `OpenAI passthrough`, `Preferences`, `Profiles`, `Prompts`, `Threads`, `Tools`, `Web search`
- Added `AI / Settings / Get the tool permission mode` and `Set the tool permission mode` (`/api/2.0/ai/config/tool-mode`)
- Added the `Files / Metadata` folder (`/api/2.0/files/metadata`): metadata templates and their fields, assigning templates to files and folders (with the cascade progress), metadata values and custom fields of files and folders, plus `Files / Folders / Search a folder by metadata` (`POST /api/2.0/files/{folderId}/search`) and `Rooms / Search the rooms by metadata` (`POST /api/2.0/files/rooms/search`)
- Added the `Apps` folder (`/api/2.0/apps`): get all apps, get an app, enable or disable an app, get and save the app settings
- Added the `Rooms / Privacy room` folder (`/api/2.0/privacyroom`): create, rotate and delete an encryption key, get the own encryption keys and the access keys of a private room
- Added the `Settings / DocsCloud` folder (`/api/2.0/settings/docscloud`): the Docs Connect tenant, its configuration (get and update), information, quota and usage, the quota report (start / status / terminate), the trial start, and the switch to Docs Connect Dev Pack with its cost calculation
- Added the status and terminate requests for the audit trail, login history and folder history reports (`GET`/`DELETE` on `/api/2.0/security/audit/events/report`, `/api/2.0/security/audit/login/report`, `/api/2.0/files/folder/{folderId}/log/report`)
- Added the wallet usage requests to `Portal / Payment`: `Get the customer service usage` and `Get the customer monthly usage`, each with its report (start / status / terminate), and `Get the service prices from the accounting service`
- Added the wallet and subscription requests `Get the active wallet services`, `Get the subscription balance information` and `Move the subscription to the wallet`
- Added the file encryption requests `Get file encryption information` and `Set file encryption information` (`/api/2.0/files/{fileId}/access`) and `Files / Sharing / Get file encryption keys` (`GET /api/2.0/files/file/{fileId}/publickeys`)
- Added the `Docs` folder (`/api/2.0/docs`): `Convert a file` (`POST /api/2.0/docs/converter`) and `Run a document builder script` (`POST /api/2.0/docs/builder`)
- Added `Files / Folders / Get the Forms section`, `Rooms / Get the .ai folder of a room`, `Files / Settings / Display quick actions`, `Settings / Webplugins / Refresh a web plugin`, `Backup / Get free and paid backup counts`, `Portal / Quota / Get upcoming payments` and `Portal / Settings / Unblock a portal` (`PUT /api/2.0/portal/unblock`)
- Added the query parameters `metadataTemplateId`, `metadataFilters` and `includeHiddenMetadata` to the folder and section listings (`Get a folder by ID`, `My documents`, `Favorites`, `Recent`, `Trash`) and to `Rooms / Get rooms`; `folderType` to `Get a folder by ID` and `Empty the Trash folder`; `privacyFilter` and `withAiFolder` to `Get rooms`; `searchArea` to `Rooms / Groups / List room groups`; `additional` to `Get the purchasable quotas`
- Added the `searchArea` body property to `Rooms / Groups / Add a new room group` and `profileId` to `AI / Agents / Create an agent` and `Update an agent`
- Added the `redirect_uris` and `scopes` body properties to `OAuth 2.0 / Client Management / Update an existing OAuth2 client`
- Added the `format`, `from` and `to` query parameters to the audit trail, login history and folder history report requests
- Added the `Accept: application/json` header to 34 requests that sent no `Accept` header; the streaming AI requests send `Accept: application/x-ndjson` (`text/event-stream` for `Stream a chat in OpenAI format`)

### Changed

- Updated the collection to the OpenAPI specification v4.0.0 (`info.version`)
- Regenerated the collection with OpenAPI Generator 7.24.0
- Moved the OAuth 2.0 client and scope requests from `/api/2.0/clients` and `/api/2.0/scopes` to `/api/2.0/oauth2/clients` and `/api/2.0/oauth2/scopes` (15 requests in `Client Management`, `Client Querying` and `Scope Management`)
- Renamed path variables, so values saved for the old names have to be entered again: `userid`/`userID` to `userId` (17 requests in `People`, `Group / Group`, `Portal / Guests`, `Portal / Users`), `productid` to `productId` (`Get product administrators`), `activationstatus` to `activationStatus` (`Set my activation status`), and `fileId`/`folderId` to `id` in `Files / Sharing / Share a file` and `Share a folder`
- Renamed the query parameters of `Settings / Security / Check product administrator` from `productid`, `userid` to `productId`, `userId`
- Turned the report requests `Generate the audit trail report`, `Generate the login history report` and `Generates folder history` into background jobs: renamed to `Start audit trail report`, `Start login history report` and `Start the folder history report generation`; poll the new status requests for the result
- Replaced the `chatSettings` body property of `AI / Agents / Create an agent` with `profileId` and `prompt`
- Made the `limit` query parameter of `OAuth 2.0 / Client Querying / List clients` optional (disabled by default)
- Renamed 223 requests to short imperative names, for example `Get ai agents` to `List agents`, `Get the backup service state` to `Check whether backups are enabled`, `Put money on deposit` to `Top up the wallet`, `OAuth2 Token Endpoint` to `Exchange the authorization code`
- Rewrote the descriptions of all 551 carried-over requests and of the folders `People / Email`, `AI / Agents`, `AI / Settings`, `AI / Vectorization`, `OAuth 2.0 / Authorization`, `Client Querying`, `Scope Management` and `Discovery`
- Updated the example values of 103 query parameters

### Removed

- Removed the superseded AI folders `Chat`, `MCP`, `Messages` and `Providers` (37 requests), replaced by the reworked AI folders
- Removed `AI / Settings / Get web search settings` and `Update web search settings` (`/api/2.0/ai/config/web-search`), superseded by the `AI / Web search` folder
- Removed the `chatRecommendedModelVisible` body property of `Update user AI settings` and the `key` and `type` body properties of `Update vectorization settings`
- Removed the `public` body property of the OAuth2 client create and update requests (`is_public` remains)
- Removed the `subjectFilter` query parameter of `Rooms / Get rooms` and `AI / Agents / List agents`
- Removed the AI balance payment requests `Credit AI balance` (`POST /api/2.0/portal/payment/creditaibalance`) and `Get the customer AI balance` (`GET /api/2.0/portal/payment/customer/aibalance`)
- Removed the Private Room requests `Files / Folders / Get the Private Room section` and `Files / Settings / Check the Private Room availability`
- Removed the `Accept: */*` header of `OAuth 2.0 / Discovery / Probe the discovery endpoint`

### Fixed

- Fixed `Files / Operations / Check the destination folder` and `Check move or copy conflicts`: the request object was sent as a single unusable `inDto` query parameter, now its fields (`destFolderId`, `folderIds`, `fileIds`, `conflictResolveType` and the rest) are separate query parameters
- Fixed query parameter values with double quotes, such as `ids` of `Settings / Security / Get module access settings`: the quotes were written as `&quot;`, so an enabled parameter sent the HTML entity to the server instead of the quote

## 3.7.0

### Added

- Added tag Rooms / Groups
- Added AI endpoints: resolve a pending editor file-generation tool (`POST /ai/chats/tool-files/{callId}/decision`), get and update per-user AI settings (`GET`/`PUT /ai/config/user`)
- Added filling forms room external DB sync endpoints: get sync status and start sync (`GET`/`POST /files/rooms/{id}/externaldbsync`)
- Added payment endpoints for AI balance: credit AI balance (`POST /portal/payment/creditaibalance`) and get the customer AI balance (`GET /portal/payment/customer/aibalance`)
- Added `PUT /files/settings/externalsharingsettings` to change the Access Control external sharing settings
- Added `sendFormToExternalDB` and `saveFormAsXLSX` properties to the room creation request
- Added `withSubFolders` query parameter to folder content listing

### Changed

- Updated SDK OpenAPI specification v3.7.0 (regenerated via OpenAPI Generator)
- File upload now uses `multipart/form-data`, with `createNewIfExist`, `storeOriginalFile`, and `keepConvertStatus` moved to query parameters
- Renamed "Get TFA confirmation URL" to "Get TFA confirmation data"
- Wallet payment amount calculation moved to `PUT /portal/payment/calculatewallet` and the checkout setup URL now accepts `successUrl`
- Payment history filters changed from list types (`types`, `status`) to single-value (`type`), and dropped the `writeOffServiceQuota` parameter
- Updated method descriptions (user existence check, password recovery with CAPTCHA note, webhook triggers) and example values

### Removed

- Removed `POST /portal/payment/buywalletservice` (buy wallet service)
- Removed `GET /portal/payment/customer/servicequota` (get the service quota)

### Fixed

- Fixed & / ' issues

## 3.6.0

- Initial release
- SDK regenerated from OpenAPI specification v3.6.0