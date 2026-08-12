# Api

All URIs are relative to *http://localhost:8092*

## Endpoints

| Class | Method | HTTP request | Description |
|------------ | ------------- | ------------- | -------------|
| *ApiKeysApi* | [**createApiKey**](#createapikey) | **POST** /api/2.0/keys | Create a user API key |
| *ApiKeysApi* | [**deleteApiKey**](#deleteapikey) | **DELETE** /api/2.0/keys/{keyId} | Delete a user API key |
| *ApiKeysApi* | [**getAllPermissions**](#getallpermissions) | **GET** /api/2.0/keys/permissions | Get API key permissions |
| *ApiKeysApi* | [**getApiKey**](#getapikey) | **GET** /api/2.0/keys/@self | Get current user's API key |
| *ApiKeysApi* | [**getApiKeys**](#getapikeys) | **GET** /api/2.0/keys | Get current user's API keys |
| *ApiKeysApi* | [**updateApiKey**](#updateapikey) | **PUT** /api/2.0/keys/{keyId} | Update an API key |
| *GroupApi* | [**addGroup**](#addgroup) | **POST** /api/2.0/group | Add a new group |
| *GroupApi* | [**addMembersTo**](#addmembersto) | **PUT** /api/2.0/group/{id}/members | Add group members |
| *GroupApi* | [**deleteGroup**](#deletegroup) | **DELETE** /api/2.0/group/{id} | Delete a group |
| *GroupApi* | [**getGroup**](#getgroup) | **GET** /api/2.0/group/{id} | Get a group |
| *GroupApi* | [**getGroupByUserId**](#getgroupbyuserid) | **GET** /api/2.0/group/user/{userid} | Get user groups |
| *GroupApi* | [**getGroups**](#getgroups) | **GET** /api/2.0/group | Get groups |
| *GroupApi* | [**moveMembersTo**](#movemembersto) | **PUT** /api/2.0/group/{fromId}/members/{toId} | Move group members |
| *GroupApi* | [**removeMembersFrom**](#removemembersfrom) | **DELETE** /api/2.0/group/{id}/members | Remove group members |
| *GroupApi* | [**setGroupManager**](#setgroupmanager) | **PUT** /api/2.0/group/{id}/manager | Set a group manager |
| *GroupApi* | [**setMembersTo**](#setmembersto) | **POST** /api/2.0/group/{id}/members | Replace group members |
| *GroupApi* | [**updateGroup**](#updategroup) | **PUT** /api/2.0/group/{id} | Update a group |
| *GroupSearchApi* | [**getGroupsWithFilesShared**](#getgroupswithfilesshared) | **GET** /api/2.0/group/file/{id} | Get groups with file sharing settings |
| *GroupSearchApi* | [**getGroupsWithFoldersShared**](#getgroupswithfoldersshared) | **GET** /api/2.0/group/folder/{id} | Get groups with folder sharing settings |
| *GroupSearchApi* | [**getGroupsWithRoomsShared**](#getgroupswithroomsshared) | **GET** /api/2.0/group/room/{id} | Get groups with room sharing settings |
| *PeopleEmailApi* | [**changeUserEmail**](#changeuseremail) | **PUT** /api/2.0/people/{userid}/email | Change a user email |
| *PeopleEmailApi* | [**sendEmailChangeInstructions**](#sendemailchangeinstructions) | **POST** /api/2.0/people/email | Send instructions to change email |
| *PeopleGuestsApi* | [**approveGuestShareLink**](#approveguestsharelink) | **POST** /api/2.0/people/guests/share/approve | Approve a guest sharing link |
| *PeopleGuestsApi* | [**deleteGuests**](#deleteguests) | **DELETE** /api/2.0/people/guests | Delete guests |
| *PeoplePasswordApi* | [**changeUserPassword**](#changeuserpassword) | **PUT** /api/2.0/people/{userid}/password | Change a user password |
| *PeoplePasswordApi* | [**sendUserPassword**](#senduserpassword) | **POST** /api/2.0/people/password | Remind a user password |
| *PeoplePhotosApi* | [**createMemberPhotoThumbnails**](#creatememberphotothumbnails) | **POST** /api/2.0/people/{userid}/photo/thumbnails | Create photo thumbnails |
| *PeoplePhotosApi* | [**deleteMemberPhoto**](#deletememberphoto) | **DELETE** /api/2.0/people/{userid}/photo | Delete a user photo |
| *PeoplePhotosApi* | [**getMemberPhoto**](#getmemberphoto) | **GET** /api/2.0/people/{userid}/photo | Get a user photo |
| *PeoplePhotosApi* | [**updateMemberPhoto**](#updatememberphoto) | **PUT** /api/2.0/people/{userid}/photo | Update a user photo |
| *PeoplePhotosApi* | [**uploadMemberPhoto**](#uploadmemberphoto) | **POST** /api/2.0/people/{userid}/photo | Upload a user photo |
| *PeopleProfilesApi* | [**addMember**](#addmember) | **POST** /api/2.0/people | Add a user |
| *PeopleProfilesApi* | [**checkUserExistsByEmail**](#checkuserexistsbyemail) | **GET** /api/2.0/people/exists | Check if a user exists by email |
| *PeopleProfilesApi* | [**deleteMember**](#deletemember) | **DELETE** /api/2.0/people/{userid} | Delete a user |
| *PeopleProfilesApi* | [**deleteProfile**](#deleteprofile) | **DELETE** /api/2.0/people/@self | Delete my profile |
| *PeopleProfilesApi* | [**getAllProfiles**](#getallprofiles) | **GET** /api/2.0/people | Get profiles |
| *PeopleProfilesApi* | [**getClaims**](#getclaims) | **GET** /api/2.0/people/tokendiagnostics | Get user claims |
| *PeopleProfilesApi* | [**getProfileByEmail**](#getprofilebyemail) | **GET** /api/2.0/people/email | Get a profile by user email |
| *PeopleProfilesApi* | [**getProfileByUserId**](#getprofilebyuserid) | **GET** /api/2.0/people/{userid} | Get a profile by user ID |
| *PeopleProfilesApi* | [**getSelfProfile**](#getselfprofile) | **GET** /api/2.0/people/@self | Get my profile |
| *PeopleProfilesApi* | [**inviteUsers**](#inviteusers) | **POST** /api/2.0/people/invite | Invite users |
| *PeopleProfilesApi* | [**removeUsers**](#removeusers) | **PUT** /api/2.0/people/delete | Delete users |
| *PeopleProfilesApi* | [**resendUserInvites**](#resenduserinvites) | **PUT** /api/2.0/people/invite | Resend activation emails |
| *PeopleProfilesApi* | [**updateMember**](#updatemember) | **PUT** /api/2.0/people/{userid} | Update a user |
| *PeopleProfilesApi* | [**updateMemberCulture**](#updatememberculture) | **PUT** /api/2.0/people/{userid}/culture | Update a user culture |
| *PeopleQuotaApi* | [**resetUsersQuota**](#resetusersquota) | **PUT** /api/2.0/people/resetquota | Reset a user quota limit |
| *PeopleQuotaApi* | [**updateUserQuota**](#updateuserquota) | **PUT** /api/2.0/people/userquota | Change a user quota limit |
| *PeopleSearchApi* | [**getAccountsEntriesWithFilesShared**](#getaccountsentrieswithfilesshared) | **GET** /api/2.0/accounts/file/{id}/search | Get account entries with file sharing settings |
| *PeopleSearchApi* | [**getAccountsEntriesWithFoldersShared**](#getaccountsentrieswithfoldersshared) | **GET** /api/2.0/accounts/folder/{id}/search | Get account entries with folder sharing settings |
| *PeopleSearchApi* | [**getAccountsEntriesWithRoomsShared**](#getaccountsentrieswithroomsshared) | **GET** /api/2.0/accounts/room/{id}/search | Get account entries |
| *PeopleSearchApi* | [**getSearch**](#getsearch) | **GET** /api/2.0/people/@search/{query} | Search users |
| *PeopleSearchApi* | [**getSimpleByFilter**](#getsimplebyfilter) | **GET** /api/2.0/people/simple/filter | Search users by extended filter |
| *PeopleSearchApi* | [**getUsersWithFilesShared**](#getuserswithfilesshared) | **GET** /api/2.0/people/file/{id} | Get users with file sharing settings |
| *PeopleSearchApi* | [**getUsersWithFoldersShared**](#getuserswithfoldersshared) | **GET** /api/2.0/people/folder/{id} | Get users with folder sharing settings |
| *PeopleSearchApi* | [**getUsersWithRoomShared**](#getuserswithroomshared) | **GET** /api/2.0/people/room/{id} | Get users with room sharing settings |
| *PeopleSearchApi* | [**searchUsersByExtendedFilter**](#searchusersbyextendedfilter) | **GET** /api/2.0/people/filter | Search users with detailed information by extended filter |
| *PeopleSearchApi* | [**searchUsersByQuery**](#searchusersbyquery) | **GET** /api/2.0/people/search | Search users (using query parameters) |
| *PeopleSearchApi* | [**searchUsersByStatus**](#searchusersbystatus) | **GET** /api/2.0/people/status/{status}/search | Search users by status filter |
| *PeopleThemeApi* | [**changePortalTheme**](#changeportaltheme) | **PUT** /api/2.0/people/theme | Change the portal theme |
| *PeopleThemeApi* | [**getPortalTheme**](#getportaltheme) | **GET** /api/2.0/people/theme | Get the portal theme |
| *PeopleThirdPartyAccountsApi* | [**getThirdPartyAuthProviders**](#getthirdpartyauthproviders) | **GET** /api/2.0/people/thirdparty/providers | Get third-party accounts |
| *PeopleThirdPartyAccountsApi* | [**linkThirdPartyAccount**](#linkthirdpartyaccount) | **PUT** /api/2.0/people/thirdparty/linkaccount | Link a third-pary account |
| *PeopleThirdPartyAccountsApi* | [**signupThirdPartyAccount**](#signupthirdpartyaccount) | **POST** /api/2.0/people/thirdparty/signup | Create a third-pary account |
| *PeopleThirdPartyAccountsApi* | [**unlinkThirdPartyAccount**](#unlinkthirdpartyaccount) | **DELETE** /api/2.0/people/thirdparty/unlinkaccount | Unlink a third-pary account |
| *PeopleUserDataApi* | [**getDeletePersonalFolderProgress**](#getdeletepersonalfolderprogress) | **GET** /api/2.0/people/delete/personal/progress | Get the progress of deleting the personal folder |
| *PeopleUserDataApi* | [**getReassignProgress**](#getreassignprogress) | **GET** /api/2.0/people/reassign/progress/{userid} | Get the reassignment progress |
| *PeopleUserDataApi* | [**getRemoveProgress**](#getremoveprogress) | **GET** /api/2.0/people/remove/progress/{userid} | Get the deletion progress |
| *PeopleUserDataApi* | [**necessaryReassign**](#necessaryreassign) | **GET** /api/2.0/people/reassign/necessary | Check data for reassignment need |
| *PeopleUserDataApi* | [**sendInstructionsToDelete**](#sendinstructionstodelete) | **PUT** /api/2.0/people/self/delete | Send the deletion instructions |
| *PeopleUserDataApi* | [**startDeletePersonalFolder**](#startdeletepersonalfolder) | **POST** /api/2.0/people/delete/personal/start | Delete the personal folder |
| *PeopleUserDataApi* | [**startReassign**](#startreassign) | **POST** /api/2.0/people/reassign/start | Start the data reassignment |
| *PeopleUserDataApi* | [**startRemove**](#startremove) | **POST** /api/2.0/people/remove/start | Start the data deletion |
| *PeopleUserDataApi* | [**terminateReassign**](#terminatereassign) | **PUT** /api/2.0/people/reassign/terminate | Terminate the data reassignment |
| *PeopleUserDataApi* | [**terminateRemove**](#terminateremove) | **PUT** /api/2.0/people/remove/terminate | Terminate the data deletion |
| *PeopleUserStatusApi* | [**getByStatus**](#getbystatus) | **GET** /api/2.0/people/status/{status} | Get profiles by status |
| *PeopleUserStatusApi* | [**updateUserActivationStatus**](#updateuseractivationstatus) | **PUT** /api/2.0/people/activationstatus/{activationstatus} | Set an activation status to the users |
| *PeopleUserStatusApi* | [**updateUserStatus**](#updateuserstatus) | **PUT** /api/2.0/people/status/{status} | Change a user status |
| *PeopleUserTypeApi* | [**getUserTypeUpdateProgress**](#getusertypeupdateprogress) | **GET** /api/2.0/people/type/progress/{userid} | Get the progress of updating user type |
| *PeopleUserTypeApi* | [**startUserTypeUpdate**](#startusertypeupdate) | **POST** /api/2.0/people/type | Start updating user type |
| *PeopleUserTypeApi* | [**terminateUserTypeUpdate**](#terminateusertypeupdate) | **PUT** /api/2.0/people/type/terminate | Terminate updating user type |
| *PeopleUserTypeApi* | [**updateUserType**](#updateusertype) | **PUT** /api/2.0/people/type/{type} | Change a user type |
| *PortalGuestsApi* | [**getGuestSharingLink**](#getguestsharinglink) | **GET** /api/2.0/people/guests/{userid}/share | Get a guest sharing link |



## ApiKeysApi

### createApiKey

> ApiKeyResponseWrapper createApiKey(CreateApiKeyRequestDto)

`POST /api/2.0/keys`

Create a user API key

Creates a user API key with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateApiKeyRequestDto** | body | [**CreateApiKeyRequestDto**](#model-createapikeyrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Create a user api key | [**ApiKeyResponseWrapper**](#model-apikeyresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ApiKeyResponseWrapper**](#model-apikeyresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteApiKey

> BooleanWrapper deleteApiKey(keyId)

`DELETE /api/2.0/keys/{keyId}`

Delete a user API key

Deletes a user API key by its ID.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **keyId** | path | **UUID** (uuid) | The API key ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Delete a user api key | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllPermissions

> STRINGArrayWrapper getAllPermissions()

`GET /api/2.0/keys/permissions`

Get API key permissions

Returns a list of all available permissions for the API key.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of all available permissions for key | [**STRINGArrayWrapper**](#model-stringarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**STRINGArrayWrapper**](#model-stringarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getApiKey

> ApiKeyResponseWrapper getApiKey()

`GET /api/2.0/keys/@self`

Get current user&#39;s API key

Returns information about the current user&#39;s API key.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of api keys for user | [**ApiKeyResponseWrapper**](#model-apikeyresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ApiKeyResponseWrapper**](#model-apikeyresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getApiKeys

> ApiKeyResponseArrayWrapper getApiKeys()

`GET /api/2.0/keys`

Get current user&#39;s API keys

Returns a list of all API keys for the current user.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of api keys for user | [**ApiKeyResponseArrayWrapper**](#model-apikeyresponsearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ApiKeyResponseArrayWrapper**](#model-apikeyresponsearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateApiKey

> BooleanWrapper updateApiKey(keyId, UpdateApiKeyRequest)

`PUT /api/2.0/keys/{keyId}`

Update an API key

Updates an existing API key changing its name, permissions, and status.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **keyId** | path | **UUID** (uuid) | The unique identifier of the API key to update. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **UpdateApiKeyRequest** | body | [**UpdateApiKeyRequest**](#model-updateapikeyrequest) | The request parameters for updating an existing API key. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Update optional params for user api keys | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## GroupApi

### addGroup

> GroupWrapper addGroup(GroupRequestDto)

`POST /api/2.0/group`

Add a new group

Adds a new group with the group manager, name, and members specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **GroupRequestDto** | body | [**GroupRequestDto**](#model-grouprequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Newly created group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### addMembersTo

> GroupWrapper addMembersTo(id, MembersRequest)

`PUT /api/2.0/group/{id}/members`

Add group members

Adds new group members to the group with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **MembersRequest** | body | [**MembersRequest**](#model-membersrequest) | The member request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteGroup

> NoContentResultWrapper deleteGroup(id)

`DELETE /api/2.0/group/{id}`

Delete a group

Deletes a group with the ID specified in the request from the list of groups on the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | No content | [**NoContentResultWrapper**](#model-nocontentresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NoContentResultWrapper**](#model-nocontentresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroup

> GroupWrapper getGroup(id, includeMembers)

`GET /api/2.0/group/{id}`

Get a group

Returns the detailed information about the selected group.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **includeMembers** | query | **Boolean** | Specifies whether to include the group members or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroupByUserId

> GroupSummaryArrayWrapper getGroupByUserId(userid)

`GET /api/2.0/group/user/{userid}`

Get user groups

Returns a list of groups for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of groups | [**GroupSummaryArrayWrapper**](#model-groupsummaryarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupSummaryArrayWrapper**](#model-groupsummaryarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroups

> GroupArrayWrapper getGroups(userId, manager, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/group`

Get groups

Returns the general information about all the groups, such as group ID and group manager.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userId** | query | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **manager** | query | **Boolean** | Specifies if the user is a manager or not. | [optional] [example: false] |
| **count** | query | **Integer** (int32) | The number of records to retrieve. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for paginated results. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the property used to sort the query results. | [optional] [example: displayName] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 0] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used for filtering or searching group data. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of groups | [**GroupArrayWrapper**](#model-grouparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupArrayWrapper**](#model-grouparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### moveMembersTo

> GroupWrapper moveMembersTo(fromId, toId)

`PUT /api/2.0/group/{fromId}/members/{toId}`

Move group members

Moves all the members from the selected group to another one specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fromId** | path | **UUID** (uuid) | The group ID to move from. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **toId** | path | **UUID** (uuid) | The group ID to move to. | [required] [example: 11111111-1111-1111-1111-111111111111] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### removeMembersFrom

> GroupWrapper removeMembersFrom(id, MembersRequest)

`DELETE /api/2.0/group/{id}/members`

Remove group members

Removes the group members specified in the request from the selected group.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **MembersRequest** | body | [**MembersRequest**](#model-membersrequest) | The member request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setGroupManager

> GroupWrapper setGroupManager(id, SetManagerRequest)

`PUT /api/2.0/group/{id}/manager`

Set a group manager

Sets a user with the ID specified in the request as a group manager.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **SetManagerRequest** | body | [**SetManagerRequest**](#model-setmanagerrequest) | The request for setting a group manager. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setMembersTo

> GroupWrapper setMembersTo(id, MembersRequest)

`POST /api/2.0/group/{id}/members`

Replace group members

Replaces the group members with those specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **MembersRequest** | body | [**MembersRequest**](#model-membersrequest) | The member request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateGroup

> GroupWrapper updateGroup(id, UpdateGroupRequest)

`PUT /api/2.0/group/{id}`

Update a group

Updates the existing group changing the group manager, name, and/or members.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **UpdateGroupRequest** | body | [**UpdateGroupRequest**](#model-updategrouprequest) | The request for updating a group. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated group with the detailed information | [**GroupWrapper**](#model-groupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Group not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupWrapper**](#model-groupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## GroupSearchApi

### getGroupsWithFilesShared

> GroupArrayWrapper getGroupsWithFilesShared(id, excludeShared, count, startIndex, filterValue)

`GET /api/2.0/group/file/{id}`

Get groups with file sharing settings

Returns groups with their sharing settings for a file with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group ID. | [required] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the group sharing settings from the response. | [optional] [example: false] |
| **count** | query | **Integer** (int32) | The number of groups to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index from which to begin retrieving groups with their sharing settings. | [optional] [example: 0] |
| **filterValue** | query | **String** | The text used as a filter for retrieving groups with their sharing settings. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**GroupArrayWrapper**](#model-grouparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupArrayWrapper**](#model-grouparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroupsWithFoldersShared

> GroupArrayWrapper getGroupsWithFoldersShared(id, excludeShared, count, startIndex, filterValue)

`GET /api/2.0/group/folder/{id}`

Get groups with folder sharing settings

Returns groups with their sharing settings in a folder with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group ID. | [required] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the group sharing settings from the response. | [optional] [example: false] |
| **count** | query | **Integer** (int32) | The number of groups to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index from which to begin retrieving groups with their sharing settings. | [optional] [example: 0] |
| **filterValue** | query | **String** | The text used as a filter for retrieving groups with their sharing settings. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**GroupArrayWrapper**](#model-grouparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupArrayWrapper**](#model-grouparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroupsWithRoomsShared

> GroupArrayWrapper getGroupsWithRoomsShared(id, excludeShared, count, startIndex, filterValue)

`GET /api/2.0/group/room/{id}`

Get groups with room sharing settings

Returns groups with their sharing settings in a room with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group ID. | [required] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the group sharing settings from the response. | [optional] [example: false] |
| **count** | query | **Integer** (int32) | The number of groups to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index from which to begin retrieving groups with their sharing settings. | [optional] [example: 0] |
| **filterValue** | query | **String** | The text used as a filter for retrieving groups with their sharing settings. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**GroupArrayWrapper**](#model-grouparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupArrayWrapper**](#model-grouparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## PeopleEmailApi

### changeUserEmail

> EmployeeFullWrapper changeUserEmail(userid, ChangeEmailRequest)

`PUT /api/2.0/people/{userid}/email`

Change a user email

Sets a new email to the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **ChangeEmailRequest** | body | [**ChangeEmailRequest**](#model-changeemailrequest) | The request parameters for updating a user email. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed user information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect userId or email | - | - |
| **403** | The link is invalid or no permissions to perform this action | - | - |
| **404** | The user could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendEmailChangeInstructions

> StringWrapper sendEmailChangeInstructions(UpdateMemberRequestDto)

`POST /api/2.0/people/email`

Send instructions to change email

Sends a message to the user email with the instructions to change the email address connected to the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMemberRequestDto** | body | [**UpdateMemberRequestDto**](#model-updatememberrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message text | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect userId or email | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PeopleGuestsApi

### approveGuestShareLink

> EmployeeFullWrapper approveGuestShareLink(EmailMemberRequestDto)

`POST /api/2.0/people/guests/share/approve`

Approve a guest sharing link

Approves a guest sharing link and returns the detailed information about a guest.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EmailMemberRequestDto** | body | [**EmailMemberRequestDto**](#model-emailmemberrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed profile information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | User not found | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteGuests

> deleteGuests(UpdateMembersRequestDto)

`DELETE /api/2.0/people/guests`

Delete guests

Deletes guests from the list and excludes them from rooms to which they were invited.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Request parameters for deleting guests | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

## PeoplePasswordApi

### changeUserPassword

> EmployeeFullWrapper changeUserPassword(userid, ChangePasswordRequest)

`PUT /api/2.0/people/{userid}/password`

Change a user password

Sets a new password to the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **ChangePasswordRequest** | body | [**ChangePasswordRequest**](#model-changepasswordrequest) | The request parameters for updating a user password. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed user information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect userId or password | - | - |
| **403** | The link is invalid or no permissions to perform this action | - | - |
| **404** | The user could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendUserPassword

> StringWrapper sendUserPassword(EmailMemberRequestDto)

`POST /api/2.0/people/password`

Remind a user password

Sends a password recovery email to the specified user address.  For unauthenticated requests, CAPTCHA validation is required when CAPTCHA is enabled in the configuration.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EmailMemberRequestDto** | body | [**EmailMemberRequestDto**](#model-emailmemberrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Email with the password | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PeoplePhotosApi

### createMemberPhotoThumbnails

> ThumbnailsDataWrapper createMemberPhotoThumbnails(userid, ThumbnailsRequest)

`POST /api/2.0/people/{userid}/photo/thumbnails`

Create photo thumbnails

Creates the user photo thumbnails by coordinates of the original image specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **ThumbnailsRequest** | body | [**ThumbnailsRequest**](#model-thumbnailsrequest) | The thumbnail request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Thumbnail parameters | [**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteMemberPhoto

> ThumbnailsDataWrapper deleteMemberPhoto(userid)

`DELETE /api/2.0/people/{userid}/photo`

Delete a user photo

Deletes a photo of the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small | [**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getMemberPhoto

> ThumbnailsDataWrapper getMemberPhoto(userid)

`GET /api/2.0/people/{userid}/photo`

Get a user photo

Returns a photo of the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small | [**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateMemberPhoto

> ThumbnailsDataWrapper updateMemberPhoto(userid, UpdatePhotoMemberRequest)

`PUT /api/2.0/people/{userid}/photo`

Update a user photo

Updates a photo of the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **UpdatePhotoMemberRequest** | body | [**UpdatePhotoMemberRequest**](#model-updatephotomemberrequest) | The request parameters for updating a photo. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated thumbnail parameters: original photo, retina, maximum size photo, big, medium, small | [**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ThumbnailsDataWrapper**](#model-thumbnailsdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### uploadMemberPhoto

> FileUploadResultWrapper uploadMemberPhoto(userid, File, Autosave)

`POST /api/2.0/people/{userid}/photo`

Upload a user photo

Uploads a photo of the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **File** | form | **File** (binary) | The image data. | [required] |
| **Autosave** | form | **Boolean** | Specifies whether to autosave a photo or not. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Result of file uploading | [**FileUploadResultWrapper**](#model-fileuploadresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The uploaded file could not be found | - | - |
| **403** | No permissions to perform this action | - | - |
| **413** | Image size is too large | - | - |
| **415** | Unknown image file type | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileUploadResultWrapper**](#model-fileuploadresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## PeopleProfilesApi

### addMember

> EmployeeFullWrapper addMember(MemberRequestDto)

`POST /api/2.0/people`

Add a user

Adds a new portal user with the first name, last name, email address, and several optional parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **MemberRequestDto** | body | [**MemberRequestDto**](#model-memberrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Newly added user with the detailed information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | The invitation link is invalid or its validity has expired | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkUserExistsByEmail

> UserExistsResponseWrapper checkUserExistsByEmail(email, encemail, culture)

`GET /api/2.0/people/exists`

Check if a user exists by email

Returns data indicating whether a user with the specified email exists on the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **email** | query | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] |
| **encemail** | query | **String** | The user encrypted email address. | [optional] [example: encrypted_email_string] |
| **culture** | query | **String** | Culture | [optional] [example: en-US] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | User existence result | [**UserExistsResponseWrapper**](#model-userexistsresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UserExistsResponseWrapper**](#model-userexistsresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteMember

> EmployeeFullWrapper deleteMember(userid)

`DELETE /api/2.0/people/{userid}`

Delete a user

Deletes a user with the ID specified in the request from the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Deleted user detailed information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation or user is not suspended | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteProfile

> EmployeeFullWrapper deleteProfile()

`DELETE /api/2.0/people/@self`

Delete my profile

Deletes the current user profile.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed information about my profile | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllProfiles

> EmployeeFullArrayWrapper getAllProfiles(count, startIndex, filterBy, sortBy, sortOrder, filterSeparator, filterValue)

`GET /api/2.0/people`

Get profiles

Returns a list of profiles for all the portal users.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **count** | query | **Integer** (int32) | The maximum number of items to be retrieved in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to be retrieved in a filtered result set. | [optional] [example: 0] |
| **filterBy** | query | **String** | Specifies the filter criteria for user-related queries. | [optional] [example: displayName] |
| **sortBy** | query | **String** | Specifies the property or field name by which the results should be sorted. | [optional] [example: displayName] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 0] [enum: 0, 1] |
| **filterSeparator** | query | **String** | The character or string used to separate multiple filter values in a filtering query. | [optional] [example: ,] |
| **filterValue** | query | **String** | The text value used as an additional filter criterion for profiles retrieval. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getClaims

> ObjectWrapper getClaims()

`GET /api/2.0/people/tokendiagnostics`

Get user claims

Returns the user claims.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Claims | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getProfileByEmail

> EmployeeFullWrapper getProfileByEmail(email, encemail, culture)

`GET /api/2.0/people/email`

Get a profile by user email

Returns the detailed information about a profile of the user with the email specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **email** | query | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] |
| **encemail** | query | **String** | The user encrypted email address. | [optional] [example: encrypted_email_string] |
| **culture** | query | **String** | Culture | [optional] [example: en-US] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed profile information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getProfileByUserId

> EmployeeFullWrapper getProfileByUserId(userid)

`GET /api/2.0/people/{userid}`

Get a profile by user ID

Returns the detailed information about a profile of the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed profile information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect UserId | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSelfProfile

> EmployeeFullWrapper getSelfProfile()

`GET /api/2.0/people/@self`

Get my profile

Returns the detailed information about the current user profile.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed information about my profile | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### inviteUsers

> EmployeeArrayWrapper inviteUsers(InviteUsersRequestDto)

`POST /api/2.0/people/invite`

Invite users

Invites users specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **InviteUsersRequestDto** | body | [**InviteUsersRequestDto**](#model-inviteusersrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users | [**EmployeeArrayWrapper**](#model-employeearraywrapper) | - |
| **400** | Incorrect email or User disabled | - | - |
| **402** | The number of admins exceeds the limit | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeArrayWrapper**](#model-employeearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### removeUsers

> EmployeeFullArrayWrapper removeUsers(UpdateMembersRequestDto)

`PUT /api/2.0/people/delete`

Delete users

Deletes a list of the users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect UserIds | - | - |
| **403** | No permissions to perform this action or users are not suspended | - | - |
| **409** | Data reassign process is not complete | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### resendUserInvites

> EmployeeFullArrayWrapper resendUserInvites(UpdateMembersRequestDto)

`PUT /api/2.0/people/invite`

Resend activation emails

Resends emails to the users who have not activated their emails.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateMember

> EmployeeFullWrapper updateMember(userid, UpdateMemberRequestDto)

`PUT /api/2.0/people/{userid}`

Update a user

Updates the data for the selected portal user with the first name, last name, email address, and/or optional parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **UpdateMemberRequestDto** | body | [**UpdateMemberRequestDto**](#model-updatememberrequestdto) | The request parameters for updating the user information. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated user with the detailed information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect user name | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateMemberCulture

> EmployeeFullWrapper updateMemberCulture(userid, Culture)

`PUT /api/2.0/people/{userid}/culture`

Update a user culture

Updates the user culture with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **String** | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **Culture** | body | [**Culture**](#model-culture) | The culture name parameters. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Detailed user information | [**EmployeeFullWrapper**](#model-employeefullwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The specified culture is not in the list of available ones | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullWrapper**](#model-employeefullwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PeopleQuotaApi

### resetUsersQuota

> EmployeeFullArrayWrapper resetUsersQuota(UpdateMembersQuotaRequestDto)

`PUT /api/2.0/people/resetquota`

Reset a user quota limit

Resets a quota limit of users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMembersQuotaRequestDto** | body | [**UpdateMembersQuotaRequestDto**](#model-updatemembersquotarequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | User detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | The invitation link is invalid or its validity has expired | - | - |
| **409** | Conflict - system user quota cannot be reset | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateUserQuota

> EmployeeFullArrayWrapper updateUserQuota(UpdateMembersQuotaRequestDto)

`PUT /api/2.0/people/userquota`

Change a user quota limit

Changes a quota limit for the users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateMembersQuotaRequestDto** | body | [**UpdateMembersQuotaRequestDto**](#model-updatemembersquotarequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The entered quota value is invalid or greater than the total storage size | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PeopleSearchApi

### getAccountsEntriesWithFilesShared

> ObjectArrayWrapper getAccountsEntriesWithFilesShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/accounts/file/{id}/search`

Get account entries with file sharing settings

Returns the account entries with their sharing settings for a file with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the account sharing settings from the response. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the account sharing settings in the response. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user is invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The area of the account entries. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of the user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in a request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | Specifies the separator used in filter expressions. | [optional] [example: ,] |
| **filterValue** | query | **String** | The text filter applied to the accounts search query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**ObjectArrayWrapper**](#model-objectarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectArrayWrapper**](#model-objectarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAccountsEntriesWithFoldersShared

> ObjectArrayWrapper getAccountsEntriesWithFoldersShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/accounts/folder/{id}/search`

Get account entries with folder sharing settings

Returns the account entries with their sharing settings in a folder with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the account sharing settings from the response. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the account sharing settings in the response. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user is invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The area of the account entries. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of the user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in a request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | Specifies the separator used in filter expressions. | [optional] [example: ,] |
| **filterValue** | query | **String** | The text filter applied to the accounts search query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**ObjectArrayWrapper**](#model-objectarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectArrayWrapper**](#model-objectarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAccountsEntriesWithRoomsShared

> ObjectArrayWrapper getAccountsEntriesWithRoomsShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/accounts/room/{id}/search`

Get account entries

Returns the account entries with their sharing settings in a room with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the account sharing settings from the response. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the account sharing settings in the response. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user is invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The area of the account entries. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of the user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in a request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | Specifies the separator used in filter expressions. | [optional] [example: ,] |
| **filterValue** | query | **String** | The text filter applied to the accounts search query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**ObjectArrayWrapper**](#model-objectarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectArrayWrapper**](#model-objectarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSearch

> EmployeeFullArrayWrapper getSearch(query, filterBy, filterValue)

`GET /api/2.0/people/@search/{query}`

Search users

Returns a list of users matching the search query.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **query** | path | **String** | The search query. | [required] [example: John] |
| **filterBy** | query | **String** | Specifies a filter criteria for the user search query. | [optional] [example: displayName] |
| **filterValue** | query | **String** | The value used for filtering users, allowing additional constraints for the query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSimpleByFilter

> EmployeeArrayWrapper getSimpleByFilter(employeeStatus, groupId, activationStatus, employeeType, employeeTypes, isAdministrator, payments, accountLoginType, quotaFilter, withoutGroup, excludeGroup, invitedByMe, inviterId, area, count, startIndex, sortBy, sortOrder, filterSeparator, filterValue)

`GET /api/2.0/people/simple/filter`

Search users by extended filter

Returns a list of users matching the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **groupId** | query | **UUID** (uuid) | The group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **employeeType** | query | **EmployeeType** | The user type. | [optional] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **employeeTypes** | query | **List** | The list of user types. | [optional] [example: [1,2]] [enum: 0, 1, 2, 3, 4] |
| **isAdministrator** | query | **Boolean** | Specifies if the user is an administrator or not. | [optional] [example: false] |
| **payments** | query | **Payments** | The user payment status. | [optional] [example: 0] [enum: 0, 1] |
| **accountLoginType** | query | **AccountLoginType** | The account login type. | [optional] [example: 0] [enum: 0, 1, 2] |
| **quotaFilter** | query | **QuotaFilter** | The quota filter (All - 0, Default - 1, Custom - 2). | [optional] [example: 0] [enum: 0, 1, 2] |
| **withoutGroup** | query | **Boolean** | Specifies whether the user should be a member of a group or not. | [optional] [example: false] |
| **excludeGroup** | query | **Boolean** | Specifies whether the user should be a member of the group with the specified ID. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user is invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The filter area. | [optional] [example: 0] [enum: 0, 1, 2] |
| **count** | query | **Integer** (int32) | The maximum number of items to be retrieved in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to be retrieved in a filtered result set. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the property or field name by which the results should be sorted. | [optional] [example: displayName] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 0] [enum: 0, 1] |
| **filterSeparator** | query | **String** | Represents the separator used to split filter criteria in query parameters. | [optional] [example: ,] |
| **filterValue** | query | **String** | The search text used to filter results based on user input. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users | [**EmployeeArrayWrapper**](#model-employeearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeArrayWrapper**](#model-employeearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUsersWithFilesShared

> EmployeeFullArrayWrapper getUsersWithFilesShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/people/file/{id}`

Get users with file sharing settings

Returns the users with the sharing settings in a file with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the user sharing settings or not. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the user sharing settings or not. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user was invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The user area. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The maximum number of users to be retrieved in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first record to retrieve in a paged query. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | The character or string used to separate multiple filter values in a filtering query. | [optional] [example: ,] |
| **filterValue** | query | **String** | The filter text value used for searching or filtering user results. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUsersWithFoldersShared

> EmployeeFullArrayWrapper getUsersWithFoldersShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/people/folder/{id}`

Get users with folder sharing settings

Returns the users with the sharing settings in a folder with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the user sharing settings or not. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the user sharing settings or not. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user was invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The user area. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The maximum number of users to be retrieved in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first record to retrieve in a paged query. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | The character or string used to separate multiple filter values in a filtering query. | [optional] [example: ,] |
| **filterValue** | query | **String** | The filter text value used for searching or filtering user results. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUsersWithRoomShared

> EmployeeFullArrayWrapper getUsersWithRoomShared(id, employeeStatus, activationStatus, excludeShared, includeShared, invitedByMe, inviterId, area, employeeTypes, count, startIndex, filterSeparator, filterValue)

`GET /api/2.0/people/room/{id}`

Get users with room sharing settings

Returns the users with the sharing settings in a room with the ID specified in request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The user ID. | [required] |
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **excludeShared** | query | **Boolean** | Specifies whether to exclude the user sharing settings or not. | [optional] [example: false] |
| **includeShared** | query | **Boolean** | Specifies whether to include the user sharing settings or not. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user was invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The user area. | [optional] [example: 0] [enum: 0, 1, 2] |
| **employeeTypes** | query | [**List**](#model-employeetype) | The list of user types. | [optional] [example: [1,2]] |
| **count** | query | **Integer** (int32) | The maximum number of users to be retrieved in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first record to retrieve in a paged query. | [optional] [example: 0] |
| **filterSeparator** | query | **String** | The character or string used to separate multiple filter values in a filtering query. | [optional] [example: ,] |
| **filterValue** | query | **String** | The filter text value used for searching or filtering user results. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### searchUsersByExtendedFilter

> EmployeeFullArrayWrapper searchUsersByExtendedFilter(employeeStatus, groupId, activationStatus, employeeType, employeeTypes, isAdministrator, payments, accountLoginType, quotaFilter, withoutGroup, excludeGroup, invitedByMe, inviterId, area, count, startIndex, sortBy, sortOrder, filterSeparator, filterValue)

`GET /api/2.0/people/filter`

Search users with detailed information by extended filter

Returns a list of users with full information about them matching the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **employeeStatus** | query | **EmployeeStatus** | The user status. | [optional] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **groupId** | query | **UUID** (uuid) | The group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **activationStatus** | query | **EmployeeActivationStatus** | The user activation status. | [optional] [example: 1] [enum: 0, 1, 2, 4] |
| **employeeType** | query | **EmployeeType** | The user type. | [optional] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **employeeTypes** | query | **List** | The list of user types. | [optional] [example: [1,2]] [enum: 0, 1, 2, 3, 4] |
| **isAdministrator** | query | **Boolean** | Specifies if the user is an administrator or not. | [optional] [example: false] |
| **payments** | query | **Payments** | The user payment status. | [optional] [example: 0] [enum: 0, 1] |
| **accountLoginType** | query | **AccountLoginType** | The account login type. | [optional] [example: 0] [enum: 0, 1, 2] |
| **quotaFilter** | query | **QuotaFilter** | The quota filter (All - 0, Default - 1, Custom - 2). | [optional] [example: 0] [enum: 0, 1, 2] |
| **withoutGroup** | query | **Boolean** | Specifies whether the user should be a member of a group or not. | [optional] [example: false] |
| **excludeGroup** | query | **Boolean** | Specifies whether the user should be a member of the group with the specified ID. | [optional] [example: false] |
| **invitedByMe** | query | **Boolean** | Specifies whether the user is invited by the current user or not. | [optional] [example: false] |
| **inviterId** | query | **UUID** (uuid) | The inviter ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **area** | query | **Area** | The filter area. | [optional] [example: 0] [enum: 0, 1, 2] |
| **count** | query | **Integer** (int32) | The maximum number of items to be retrieved in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to be retrieved in a filtered result set. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the property or field name by which the results should be sorted. | [optional] [example: displayName] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 0] [enum: 0, 1] |
| **filterSeparator** | query | **String** | Represents the separator used to split filter criteria in query parameters. | [optional] [example: ,] |
| **filterValue** | query | **String** | The search text used to filter results based on user input. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### searchUsersByQuery

> EmployeeArrayWrapper searchUsersByQuery(query)

`GET /api/2.0/people/search`

Search users (using query parameters)

Returns a list of users matching the search query. This method uses the query parameters.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **query** | query | **String** | The search query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users | [**EmployeeArrayWrapper**](#model-employeearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeArrayWrapper**](#model-employeearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### searchUsersByStatus

> EmployeeFullArrayWrapper searchUsersByStatus(status, query, filterBy, filterValue)

`GET /api/2.0/people/status/{status}/search`

Search users by status filter

Returns a list of users matching the status filter and search query.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **status** | path | **EmployeeStatus** | The user status. | [required] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **query** | query | **String** | The advanced search query. | [optional] [example: John] |
| **filterBy** | query | **String** | Specifies the criteria used to filter search results in advanced queries. | [optional] [example: displayName] |
| **filterValue** | query | **String** | The value used to filter the search query. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## PeopleThemeApi

### changePortalTheme

> DarkThemeSettingsWrapper changePortalTheme(DarkThemeSettingsRequestDto)

`PUT /api/2.0/people/theme`

Change the portal theme

Changes the current portal theme.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DarkThemeSettingsRequestDto** | body | [**DarkThemeSettingsRequestDto**](#model-darkthemesettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Theme | [**DarkThemeSettingsWrapper**](#model-darkthemesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DarkThemeSettingsWrapper**](#model-darkthemesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getPortalTheme

> DarkThemeSettingsWrapper getPortalTheme()

`GET /api/2.0/people/theme`

Get the portal theme

Returns a theme which is set to the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Theme | [**DarkThemeSettingsWrapper**](#model-darkthemesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DarkThemeSettingsWrapper**](#model-darkthemesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## PeopleThirdPartyAccountsApi

### getThirdPartyAuthProviders

> AccountInfoArrayWrapper getThirdPartyAuthProviders(inviteView, settingsView, clientCallback, fromOnly)

`GET /api/2.0/people/thirdparty/providers`

Get third-party accounts

Returns a list of the available third-party accounts.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **inviteView** | query | **Boolean** | Specifies whether to return providers that are available for invitation links, i.e. the user can login or register through these providers. | [optional] [example: false] |
| **settingsView** | query | **Boolean** | Specifies whether to display the provider settings in a pop-up window (true) or redirect them to the desktop application (false). | [optional] [example: false] |
| **clientCallback** | query | **String** | The method that is called after authentication. | [optional] [example: onAuthCallback] |
| **fromOnly** | query | **String** | The provider name if a response is required only from this provider. | [optional] [example: Google] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of third-party accounts | [**AccountInfoArrayWrapper**](#model-accountinfoarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AccountInfoArrayWrapper**](#model-accountinfoarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### linkThirdPartyAccount

> linkThirdPartyAccount(LinkAccountRequestDto)

`PUT /api/2.0/people/thirdparty/linkaccount`

Link a third-pary account

Links a third-party account specified in the request to the user profile.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **LinkAccountRequestDto** | body | [**LinkAccountRequestDto**](#model-linkaccountrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | Error not allowed option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### signupThirdPartyAccount

> EmployeeWrapper signupThirdPartyAccount(SignupAccountRequestDto)

`POST /api/2.0/people/thirdparty/signup`

Create a third-pary account

Creates a third-party account with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SignupAccountRequestDto** | body | [**SignupAccountRequestDto**](#model-signupaccountrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**EmployeeWrapper**](#model-employeewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email | - | - |
| **403** | The invitation link is invalid or its validity has expired | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeWrapper**](#model-employeewrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### unlinkThirdPartyAccount

> unlinkThirdPartyAccount(provider)

`DELETE /api/2.0/people/thirdparty/unlinkaccount`

Unlink a third-pary account

Unlinks a third-party account specified in the request from the user profile.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **provider** | query | **String** | The provider name. | [optional] [example: Google] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

## PeopleUserDataApi

### getDeletePersonalFolderProgress

> TaskProgressResponseWrapper getDeletePersonalFolderProgress()

`GET /api/2.0/people/delete/personal/progress`

Get the progress of deleting the personal folder

Returns the progress of deleting the personal folder.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Deletion progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getReassignProgress

> TaskProgressResponseWrapper getReassignProgress(userid)

`GET /api/2.0/people/reassign/progress/{userid}`

Get the reassignment progress

Returns the progress of the started data reassignment for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Reassignment progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRemoveProgress

> TaskProgressResponseWrapper getRemoveProgress(userid)

`GET /api/2.0/people/remove/progress/{userid}`

Get the deletion progress

Returns the progress of the started data deletion for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Deletion progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### necessaryReassign

> BooleanWrapper necessaryReassign(UserId, Type)

`GET /api/2.0/people/reassign/necessary`

Check data for reassignment need

Checks whether the reassignment of rooms and shared files is required.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UserId** | query | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **Type** | query | **EmployeeType** | The expected user type. | [optional] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if neccessary reassign | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### sendInstructionsToDelete

> StringWrapper sendInstructionsToDelete()

`PUT /api/2.0/people/self/delete`

Send the deletion instructions

Sends the instructions for deleting a user profile.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Information message | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startDeletePersonalFolder

> TaskProgressResponseWrapper startDeletePersonalFolder()

`POST /api/2.0/people/delete/personal/start`

Delete the personal folder

Starts deleting the personal folder.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | delete personal progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startReassign

> TaskProgressResponseWrapper startReassign(StartReassignRequestDto)

`POST /api/2.0/people/reassign/start`

Start the data reassignment

Starts the data reassignment for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **StartReassignRequestDto** | body | [**StartReassignRequestDto**](#model-startreassignrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Reassignment progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Can not reassign data to user or from user | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startRemove

> TaskProgressResponseWrapper startRemove(TerminateRequestDto)

`POST /api/2.0/people/remove/start`

Start the data deletion

Starts the data deletion for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TerminateRequestDto** | body | [**TerminateRequestDto**](#model-terminaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Deletion progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | User exception | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | User not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateReassign

> TaskProgressResponseWrapper terminateReassign(TerminateRequestDto)

`PUT /api/2.0/people/reassign/terminate`

Terminate the data reassignment

Terminates the data reassignment for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TerminateRequestDto** | body | [**TerminateRequestDto**](#model-terminaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Reassignment progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateRemove

> terminateRemove(TerminateRequestDto)

`PUT /api/2.0/people/remove/terminate`

Terminate the data deletion

Terminates the data deletion for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TerminateRequestDto** | body | [**TerminateRequestDto**](#model-terminaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

## PeopleUserStatusApi

### getByStatus

> EmployeeFullArrayWrapper getByStatus(status, filterBy, count, startIndex, sortBy, sortOrder, filterSeparator, filterValue)

`GET /api/2.0/people/status/{status}`

Get profiles by status

Returns a list of profiles filtered by the user status.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **status** | path | **EmployeeStatus** | The user status. | [required] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **filterBy** | query | **String** | Specifies the criteria used to filter the profiles in the request. | [optional] [example: displayName] |
| **count** | query | **Integer** (int32) | The maximum number of user profiles to retrieve. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for retrieving data in a paginated request. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the property or field name by which the results should be sorted. | [optional] [example: displayName] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 0] [enum: 0, 1] |
| **filterSeparator** | query | **String** | Represents the separator used to split multiple filter criteria in a query string. | [optional] [example: ,] |
| **filterValue** | query | **String** | A string value representing additional filter criteria used in query parameters. | [optional] [example: John] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateUserActivationStatus

> EmployeeFullArrayWrapper updateUserActivationStatus(activationstatus, UpdateMembersRequestDto)

`PUT /api/2.0/people/activationstatus/{activationstatus}`

Set an activation status to the users

Sets the required activation status to the list of users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **activationstatus** | path | **EmployeeActivationStatus** | The new user activation status. | [required] [example: 1] [enum: 0, 1, 2, 4] |
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) | The request parameters for updating the user information. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateUserStatus

> EmployeeFullArrayWrapper updateUserStatus(status, UpdateMembersRequestDto)

`PUT /api/2.0/people/status/{status}`

Change a user status

Changes a status of the users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **status** | path | **EmployeeStatus** | The new user status. | [required] [example: 1] [enum: 1, 2, 4, 5, 7] |
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) | The request parameters for updating the user information. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect status | - | - |
| **403** | No permissions to perform this action or cannot change status for a specific user (yourself, owner, LDAP ...) | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PeopleUserTypeApi

### getUserTypeUpdateProgress

> TaskProgressResponseWrapper getUserTypeUpdateProgress(userid)

`GET /api/2.0/people/type/progress/{userid}`

Get the progress of updating user type

Returns the progress of updating the user type.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Update type progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startUserTypeUpdate

> TaskProgressResponseWrapper startUserTypeUpdate(StartUpdateUserTypeDto)

`POST /api/2.0/people/type`

Start updating user type

Starts updating the type of the user or guest when reassigning rooms and shared files.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **StartUpdateUserTypeDto** | body | [**StartUpdateUserTypeDto**](#model-startupdateusertypedto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Update type progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Can not update user type | - | - |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateUserTypeUpdate

> TaskProgressResponseWrapper terminateUserTypeUpdate(TerminateRequestDto)

`PUT /api/2.0/people/type/terminate`

Terminate updating user type

Terminates the process of updating the type of the user or guest.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TerminateRequestDto** | body | [**TerminateRequestDto**](#model-terminaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Update type progress | [**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TaskProgressResponseWrapper**](#model-taskprogressresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateUserType

> EmployeeFullArrayWrapper updateUserType(type, UpdateMembersRequestDto)

`PUT /api/2.0/people/type/{type}`

Change a user type

Changes a type of the users with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **type** | path | **EmployeeType** | The new user type. | [required] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **UpdateMembersRequestDto** | body | [**UpdateMembersRequestDto**](#model-updatemembersrequestdto) | The request parameters for updating the user information. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with the detailed information | [**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeFullArrayWrapper**](#model-employeefullarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PortalGuestsApi

### getGuestSharingLink

> StringWrapper getGuestSharingLink(userid)

`GET /api/2.0/people/guests/{userid}/share`

Get a guest sharing link

Returns a link to share a guest with another user.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userid** | path | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | User share link | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | User not found | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json


## Models


### Model AccountInfoArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-accountinfodto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AccountInfoDto
The account information parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **provider** | **String** | The account provider. | [required] [example: Google] [nullable] |
| **url** | **URI** (uri) | The account URL. | [required] [example: https://example.com/account] [nullable] |
| **linked** | **Boolean** | Specifies if an account is linked with other profiles or not. | [required] [example: true] |


### Model AccountLoginType
[0 - SSO, 1 - LDAP, 2 - Standart]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model ApiDateTime
The API date and time parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **utcTime** | **Date** (date-time) | The time in UTC format. | [optional] [example: 2018-01-01T00:00:00Z] |
| **timeZoneOffset** | **String** (date-span) | The time zone offset. | [optional] [example: 00:00:00] |


### Model ApiKeyResponseArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-apikeyresponsedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ApiKeyResponseDto
The response data for the API key operations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The API key unique identifier. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **name** | **String** | The API key name. | [required] [example: My API Key] [nullable] |
| **key** | **String** | The full API key value (only returned when creating a new key). | [required] [example: api_key_1234567890abcdef] [nullable] |
| **keyPostfix** | **String** | The API key postfix (used for identification). | [optional] [example: ...cdef] [nullable] |
| **permissions** | **List** | The list of permissions granted to the API key. | [required] [example: ["read","write","delete"]] [nullable] |
| **lastUsed** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createOn** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **expiresAt** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isActive** | **Boolean** | Indicates whether the API key is active or not. | [required] [example: true] |


### Model ApiKeyResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ApiKeyResponseDto**](#model-apikeyresponsedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Area
[0 - All, 1 - People, 2 - Guests]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BooleanWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Boolean** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BooleanWrapper.links item

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **href** | **String** | URL of the link | [optional] |
| **action** | **String** | Action associated with the link | [optional] |


### Model ChangeEmailRequest
The request parameters for updating a user email.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **encEmail** | **String** | The user encrypted email address. | [optional] [example: encrypted_email_string] [nullable] |


### Model ChangePasswordRequest
The request parameters for updating a user password.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **password** | **String** | The user password. | [optional] [example: P@ssw0rd] [nullable] |
| **passwordHash** | **String** | The user password hash. | [optional] [example: 5f4dcc3b5aa765d61d8327deb882cf99] [nullable] |


### Model Contact
The contact information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | **String** | The contact type. | [optional] [example: GTalk] [nullable] |
| **value** | **String** | The contact value. | [optional] [example: my@gmail.com] [nullable] |


### Model CreateApiKeyRequestDto
The request parameters for creating a new API key.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The API key name. | [required] [example: My API Key] [minLength: 0] [maxLength: 30] |
| **permissions** | **List** | The list of permissions granted to the API key. | [optional] [example: ["read","write"]] [nullable] |
| **expiresInDays** | **Integer** (int32) | The number of days until the API key expires (null for no expiration). | [optional] [example: 30] [min: 1] [max: 365] [nullable] |


### Model Culture
The culture name parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **cultureName** | **String** | The user culture name (en-US, de, fr, es, ...). | [required] [example: en-US] [minLength: 0] [maxLength: 85] |


### Model DarkThemeSettings
The theme parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **theme** | [**DarkThemeSettingsType**](#model-darkthemesettingstype) |  | [optional] [enum: Base, Dark, System] |
| **lastModified** | **Date** (date-time) | The last modified date. | [optional] [example: 2020-01-15T00:00:00Z] |


### Model DarkThemeSettingsRequestDto
The theme settings request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **theme** | [**DarkThemeSettingsType**](#model-darkthemesettingstype) |  | [required] [enum: Base, Dark, System] |


### Model DarkThemeSettingsType
[Base - Base, Dark - Dark, System - System]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DarkThemeSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DarkThemeSettings**](#model-darkthemesettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DistributedTaskStatus
[0 - Created, 1 - Running, 2 - Completed, 3 - Canceled, 4 - Failted]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmailInvitationDto
The email invitation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address. | [optional] [example: user@example.com] [maxLength: 255] [nullable] |


### Model EmailMemberRequestDto
The request parameters for the user email.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The user email address. | [required] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] |
| **recaptchaType** | [**RecaptchaType**](#model-recaptchatype) |  | [optional] [enum: 0, 1, 2, 3] |
| **recaptchaResponse** | **String** | The user&#39;s response to the CAPTCHA challenge. | [optional] [example: 03AGdBq27...] [nullable] |


### Model EmployeeActivationStatus
[0 - Not activated, 1 - Activated, 2 - Pending, 4 - Auto generated]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmployeeArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-employeedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmployeeDto
The user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **displayName** | **String** | The HTML-encoded user&#39;s display name formatted according to the default format for the current culture. | [optional] [example: Mike Zanyatski] [nullable] |
| **avatar** | **String** | The user avatar. | [optional] [example: https://example.com/avatar.jpg] [nullable] |
| **avatarOriginal** | **String** | The user original size avatar. | [optional] [example: https://example.com/avatar_original.jpg] [nullable] |
| **avatarMax** | **String** | The user maximum size avatar. | [optional] [example: https://example.com/avatar_max.jpg] [nullable] |
| **avatarMedium** | **String** | The user medium size avatar. | [optional] [example: https://example.com/avatar_medium.jpg] [nullable] |
| **avatarSmall** | **String** | The user small size avatar. | [optional] [example: https://example.com/avatar_small.jpg] [nullable] |
| **profileUrl** | **String** | The user profile URL. | [optional] [example: https://example.com/profile/user123] [nullable] |
| **hasAvatar** | **Boolean** | Specifies if the user has an avatar or not. | [optional] [example: true] |
| **isAnonim** | **Boolean** | Specifies if the user is anonymous or not. | [optional] [example: false] |


### Model EmployeeFullArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-employeefulldto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmployeeFullDto
The full list of user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] |
| **displayName** | **String** | The HTML-encoded user&#39;s display name formatted according to the default format for the current culture. | [optional] [nullable] |
| **avatar** | **String** | The user avatar. | [optional] [nullable] |
| **avatarOriginal** | **String** | The user original size avatar. | [optional] [nullable] |
| **avatarMax** | **String** | The user maximum size avatar. | [optional] [nullable] |
| **avatarMedium** | **String** | The user medium size avatar. | [optional] [nullable] |
| **avatarSmall** | **String** | The user small size avatar. | [optional] [nullable] |
| **profileUrl** | **String** | The user profile URL. | [optional] [nullable] |
| **hasAvatar** | **Boolean** | Specifies if the user has an avatar or not. | [optional] |
| **isAnonim** | **Boolean** | Specifies if the user is anonymous or not. | [optional] |
| **firstName** | **String** | The user first name. | [optional] [nullable] |
| **lastName** | **String** | The user last name. | [optional] [nullable] |
| **userName** | **String** | The user username. | [optional] [nullable] |
| **email** | **String** (email) | The user email. | [optional] [nullable] |
| **contacts** | [**List**](#model-contact) | The list of user contacts. | [optional] [nullable] |
| **status** | [**EmployeeStatus**](#model-employeestatus) |  | [optional] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | [**EmployeeActivationStatus**](#model-employeeactivationstatus) |  | [optional] [enum: 0, 1, 2, 4] |
| **terminated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **department** | **String** | The user department. | [optional] [nullable] |
| **groups** | [**List**](#model-groupsummarydto) | The list of user groups. | [optional] [nullable] |
| **location** | **String** | The user location. | [optional] [nullable] |
| **notes** | **String** | The user notes. | [optional] [nullable] |
| **isAdmin** | **Boolean** | Specifies if the user is an administrator or not. | [optional] |
| **isRoomAdmin** | **Boolean** | Specifies if the user is a room administrator or not. | [optional] |
| **isLDAP** | **Boolean** | Specifies if the LDAP settings are enabled for the user or not. | [optional] |
| **listAdminModules** | **List** | The list of the administrator modules. | [optional] [nullable] |
| **isOwner** | **Boolean** | Specifies if the user is a portal owner or not. | [optional] |
| **isVisitor** | **Boolean** | Specifies if the user is a portal visitor or not. | [optional] |
| **isCollaborator** | **Boolean** | Specifies if the user is a portal collaborator or not. | [optional] |
| **cultureName** | **String** | The user culture code. | [optional] [nullable] |
| **mobilePhone** | **String** | The user mobile phone number. | [optional] [nullable] |
| **mobilePhoneActivationStatus** | [**MobilePhoneActivationStatus**](#model-mobilephoneactivationstatus) |  | [optional] [enum: 0, 1] |
| **isSSO** | **Boolean** | Specifies if the SSO settings are enabled for the user or not. | [optional] |
| **theme** | [**DarkThemeSettingsType**](#model-darkthemesettingstype) |  | [optional] [enum: Base, Dark, System] |
| **quotaLimit** | **Long** (int64) | The user quota limit. | [optional] [nullable] |
| **usedSpace** | **Double** (double) | The portal used space of the user. | [optional] [nullable] |
| **shared** | **Boolean** | Specifies if the user has access rights. | [optional] [nullable] |
| **isCustomQuota** | **Boolean** | Specifies if the user has a custom quota or not. | [optional] [nullable] |
| **loginEventId** | **Integer** (int32) | The current login event ID. | [optional] [nullable] |
| **authCookieLifetime** | **Double** (double) | The auth cookie lifetime in seconds. | [optional] [nullable] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **registrationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **hasPersonalFolder** | **Boolean** | Specifies if the user has a personal folder or not. | [optional] [nullable] |
| **tfaAppEnabled** | **Boolean** | Indicates whether the user has enabled two-factor authentication (TFA) using an authentication app. | [optional] [nullable] |


### Model EmployeeFullWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmployeeStatus
[1 - Active, 2 - Terminated, 4 - Pending, 5 - Default, 7 - All]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmployeeType
[All - All, RoomAdmin - Room admin, Guest - Guest, DocSpaceAdmin - DocSpace admin, User - User]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmployeeWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileUploadResultDto
The file upload result.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **success** | **Boolean** | Specifies if the upload operation is successful or not. | [optional] [example: true] |
| **data** | **oas_any_type_not_mapped** | The file upload result data. | [optional] [example: {"fileId":"123","fileName":"photo.jpg"}] [nullable] |
| **message** | **String** | The file upload result message. | [optional] [example: File uploaded successfully] [nullable] |


### Model FileUploadResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileUploadResultDto**](#model-fileuploadresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model GroupArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-groupdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model GroupDto
The group parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The group name. | [required] [example: Marketing Team] [nullable] |
| **parent** | **UUID** (uuid) | The parent group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **category** | **UUID** (uuid) | The group category ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **id** | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **isLDAP** | **Boolean** | Specifies if the LDAP settings are enabled for the group or not. | [required] [example: false] |
| **isSystem** | **Boolean** | Indicates whether the group is a system group. | [optional] [example: false] [nullable] |
| **manager** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **members** | [**List**](#model-employeefulldto) | The list of group members. | [optional] [example: [{"displayName":"John Doe"}]] [nullable] |
| **shared** | **Boolean** | Specifies whether the group can be shared or not. | [optional] [example: false] [nullable] |
| **membersCount** | **Integer** (int32) | The number of group members. | [optional] [example: 0] |


### Model GroupRequestDto
The group request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **members** | **List** (uuid) | The list of group member IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000","11111111-1111-1111-1111-111111111111"]] [nullable] |
| **groupManager** | **UUID** (uuid) | The group manager ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **groupName** | **String** | The group name. | [required] [example: Marketing Team] [minLength: 1] [maxLength: 128] [nullable] |


### Model GroupSummaryArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-groupsummarydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model GroupSummaryDto
The group summary parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **name** | **String** | The group name. | [required] [example: Group Name] [nullable] |
| **manager** | **String** | The group manager. | [optional] [example: Jake.Zazhitski] [nullable] |
| **isSystem** | **Boolean** | Indicates whether the group is a system group. | [optional] [example: false] [nullable] |


### Model GroupWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**GroupDto**](#model-groupdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model InviteUsersRequestDto
The request parameters for inviting users.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **invitations** | [**List**](#model-userinvitationrequestdto) | The list of user invitations. | [required] [example: [{"email":"user@example.com","type":1}]] |
| **culture** | **String** | The culture code of invitations. | [optional] [example: en-US] [nullable] |


### Model LinkAccountRequestDto
The request parameters for linking accounts.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **serializedProfile** | **String** | The third-party profile in the serialized format. | [optional] [example: {"provider":"Google","id":"123456"}] [nullable] |


### Model MemberRequestDto
The user request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **password** | **String** | The user password. | [optional] [example: P@ssw0rd] [nullable] |
| **passwordHash** | **String** | The user password hash. | [optional] [example: 5f4dcc3b5aa765d61d8327deb882cf99] [nullable] |
| **email** | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **type** | [**EmployeeType**](#model-employeetype) |  | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **isUser** | **Boolean** | Specifies if this is a guest or a user. | [optional] [example: true] [nullable] |
| **firstName** | **String** | The user first name. | [optional] [example: John] [minLength: 0] [maxLength: 255] [nullable] |
| **lastName** | **String** | The user last name. | [optional] [example: Doe] [minLength: 0] [maxLength: 255] [nullable] |
| **department** | **List** (uuid) | The list of the user departments IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000"]] [nullable] |
| **location** | **String** | The user location. | [optional] [example: New York] [nullable] |
| **comment** | **String** | The user comment. | [optional] [example: User comment] [nullable] |
| **contacts** | [**List**](#model-contact) | The list of the user contacts. | [optional] [example: [{"type":"email","value":"john.doe@example.com"}]] [nullable] |
| **files** | **String** | The avatar photo URL. | [optional] [example: https://example.com/avatar.jpg] [nullable] |
| **fromInviteLink** | **Boolean** | Specifies if the user is added via the invitation link or not. | [optional] [example: false] |
| **key** | **String** | The user key. | [optional] [example: user_key_string] [nullable] |
| **cultureName** | **String** | The user culture code. | [optional] [example: en-US] [nullable] |
| **target** | **UUID** (uuid) | The user target ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **spam** | **Boolean** | Specifies if tips, updates and offers are allowed to be sent to the user or not. | [optional] [example: false] [nullable] |


### Model MembersRequest
The member request.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **members** | **List** (uuid) | The list of group member IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000","11111111-1111-1111-1111-111111111111"]] [nullable] |


### Model MobilePhoneActivationStatus
[0 - Not activated, 1 - Activated]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model NoContentResult

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **statusCode** | **Integer** (int32) |  | [optional] |


### Model NoContentResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**NoContentResult**](#model-nocontentresult) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ObjectArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ObjectWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Object** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Payments
[0 - Paid, 1 - Free]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model QuotaFilter
[0 - All, 1 - Default, 2 - Custom]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model RecaptchaType
[0 - Default, 1 - AndroidV2, 2 - iOSV2, 3 - hCaptcha]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model STRINGArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SetManagerRequest
The request for setting a group manager.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |


### Model SignupAccountRequestDto
The request parameters for creating a third-party account.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **employeeType** | [**EmployeeType**](#model-employeetype) |  | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **key** | **String** | The user link key. | [required] [example: invite_key_123456] [nullable] |
| **culture** | **String** | The user culture code. | [optional] [example: en-US] [nullable] |
| **serializedProfile** | **String** | The third-party profile in the serialized format | [required] [example: {"provider":"Google","id":"123456"}] [nullable] |


### Model SortOrder
[0 - Ascending, 1 - Descending]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model StartReassignRequestDto
The request parameters for starting the reassignment process.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fromUserId** | **UUID** (uuid) | The user ID whose data will be reassigned to another user. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **toUserId** | **UUID** (uuid) | The user ID to whom all the data will be reassigned. | [required] [example: 11111111-1111-1111-1111-111111111111] |
| **deleteProfile** | **Boolean** | Specifies whether to delete a profile when the data reassignment will be finished or not. | [optional] [example: false] |


### Model StartUpdateUserTypeDto
The parameters for updating the type of the user or guest when reassigning rooms and shared files.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**EmployeeType**](#model-employeetype) |  | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **userId** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **reassignUserId** | **UUID** (uuid) | The user ID to reassign. | [optional] [example: 11111111-1111-1111-1111-111111111111] [nullable] |


### Model StringWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **String** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TaskProgressResponseDto
The task progress response parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The task progress ID. | [required] [example: task-123456] [nullable] |
| **error** | **String** | The task progress error message. | [optional] [example: An error occurred during processing] [nullable] |
| **percentage** | **Integer** (int32) | The percentage of the task progress. | [required] [example: 75] |
| **isCompleted** | **Boolean** | Specifies if the task peogress is completed or not. | [required] [example: false] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) |  | [required] [enum: 0, 1, 2, 3, 4] |


### Model TaskProgressResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TaskProgressResponseDto**](#model-taskprogressresponsedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TerminateRequestDto
The request parameters for terminating the reassignment/deletion process.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **UUID** (uuid) | The user ID whose data is reassigned/removed. | [required] [example: 00000000-0000-0000-0000-000000000000] |


### Model ThumbnailsDataDto
The thumbnails data parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **original** | **String** | The thumbnail original photo. | [optional] [example: default_user_photo_size_1280-1280.png] [nullable] |
| **retina** | **String** | The thumbnail retina. | [optional] [example: default_user_photo_size_360-360.png] [nullable] |
| **max** | **String** | The thumbnail maximum size photo. | [optional] [example: default_user_photo_size_200-200.png] [nullable] |
| **big** | **String** | The thumbnail big size photo. | [optional] [example: default_user_photo_size_82-82.png] [nullable] |
| **medium** | **String** | The thumbnail medium size photo. | [optional] [example: default_user_photo_size_48-48.png] [nullable] |
| **small** | **String** | The thumbnail small size photo. | [optional] [example: default_user_photo_size_32-32.png] [nullable] |


### Model ThumbnailsDataWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ThumbnailsDataDto**](#model-thumbnailsdatadto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ThumbnailsRequest
The thumbnail request.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tmpFile** | **String** | The path to the temporary thumbnail file. | [optional] [example: /tmp/photo_temp_123.jpg] [nullable] |
| **x** | **Integer** (int32) | The thumbnail horizontal coordinate. | [optional] [example: 100] |
| **y** | **Integer** (int32) | The thumbnail vertical coordinate. | [optional] [example: 50] |
| **width** | **Integer** (int32) | The thumbnail width. | [optional] [example: 200] |
| **height** | **Integer** (int32) | The thumbnail height. | [optional] [example: 200] |


### Model UpdateApiKeyRequest
The request parameters for updating an existing API key.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The new name for the API key. | [optional] [example: Updated API Key] [minLength: 0] [maxLength: 30] [nullable] |
| **permissions** | **List** | The new list of permissions for the API key. | [optional] [example: ["read","write","delete"]] [nullable] |
| **isActive** | **Boolean** | Indicates whether the API key should be active or not. | [optional] [example: true] [nullable] |


### Model UpdateGroupRequest
The request for updating a group.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **membersToAdd** | **List** (uuid) | The list of user IDs to add to the group. | [optional] [example: ["00000000-0000-0000-0000-000000000000"]] [nullable] |
| **membersToRemove** | **List** (uuid) | The list of user IDs to remove from the group. | [optional] [example: ["11111111-1111-1111-1111-111111111111"]] [nullable] |
| **groupManager** | **UUID** (uuid) | The group manager ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **groupName** | **String** | The group name. | [optional] [example: Sales Team] [minLength: 0] [maxLength: 128] [nullable] |


### Model UpdateMemberRequestDto
The request parameters for updating the user information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **String** | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **disable** | **Boolean** | Specifies whether to disable a user or not. | [optional] [example: false] [nullable] |
| **email** | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **isUser** | **Boolean** | Specifies if this is a guest or a user. | [optional] [example: true] [nullable] |
| **firstName** | **String** | The user first name. | [optional] [example: John] [minLength: 0] [maxLength: 255] [nullable] |
| **lastName** | **String** | The user last name. | [optional] [example: Doe] [minLength: 0] [maxLength: 255] [nullable] |
| **department** | **List** (uuid) | The list of the user departments. | [optional] [example: ["00000000-0000-0000-0000-000000000000"]] [nullable] |
| **location** | **String** | The user location. | [optional] [example: New York] [nullable] |
| **comment** | **String** | The user comment. | [optional] [example: User comment] [nullable] |
| **contacts** | [**List**](#model-contact) | The list of the user contacts. | [optional] [example: [{"type":"email","value":"john.doe@example.com"}]] [nullable] |
| **files** | **String** | The user avatar photo URL. | [optional] [example: https://example.com/avatar.jpg] [nullable] |
| **spam** | **Boolean** | Specifies if tips, updates and offers are allowed to be sent to the user or not. | [optional] [example: false] [nullable] |


### Model UpdateMembersQuotaRequestDto
The request parameters for updating a user quota.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userIds** | **List** (uuid) | The list of user IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000","11111111-1111-1111-1111-111111111111"]] [nullable] |
| **quota** | [**UpdateMembersQuotaRequestDto_quota**](#model-updatemembersquotarequestdtoquota) |  | [optional] |


### Model UpdateMembersQuotaRequestDto.quota
The quota in JSON format.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model UpdateMembersRequestDto
The request parameters for updating the user information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userIds** | **List** (uuid) | The list of user IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000","11111111-1111-1111-1111-111111111111"]] [nullable] |
| **resendAll** | **Boolean** | Specifies whether to resend invitation letters to all the users or not. | [optional] [example: false] |


### Model UpdatePhotoMemberRequest
The request parameters for updating a photo.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **files** | **String** | The avatar photo URL. | [optional] [example: https://example.com/avatar.jpg] [nullable] |


### Model UserExistsResponseDto
The user existence check response parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **exists** | **Boolean** | Specifies whether the user exists or not. | [required] [example: true] |
| **status** | [**EmployeeStatus**](#model-employeestatus) |  | [optional] [enum: 1, 2, 4, 5, 7] |


### Model UserExistsResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**UserExistsResponseDto**](#model-userexistsresponsedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model UserInvitationRequestDto
The user invitation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address. | [optional] [maxLength: 255] [nullable] |
| **type** | [**EmployeeType**](#model-employeetype) |  | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |


## Authorization


### asc_auth_key
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### Basic

- **Type**: HTTP basic authentication


### Bearer

- **Type**: HTTP Bearer Token authentication (JWT)


### ApiKeyBearer
- **Type**: API key
- **API key parameter name**: ApiKeyBearer
- **Location**: HTTP header


### OAuth2

- **Type**: OAuth
- **Flow**: accessCode
- **Authorization URL**: 
- **Scopes**: 
  - read: Read access to protected resources
  - write: Write access to protected resources


### OpenId


### cookieAuth
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### bearerAuth

- **Type**: HTTP Bearer Token authentication


### x-signature
- **Type**: API key
- **API key parameter name**: x-signature
- **Location**: 

