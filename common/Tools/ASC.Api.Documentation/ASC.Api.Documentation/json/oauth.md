# ONLYOFFICE DocSpace OAuth 2.0 API

The browsable version of this reference, with a request builder and code samples, is published at
<https://api.onlyoffice.com/docspace/api-backend/usage-api/>.

All URIs are relative to *https://yourportal.onlyoffice.com*, where the host is the address of your DocSpace instance.

## Endpoints

| Class | Method | HTTP request | Description |
|------------ | ------------- | ------------- | -------------|
| *OAuth20AuthorizationApi* | [**authorizeOAuth**](#authorizeoauth) | **GET** /oauth2/authorize | OAuth2 Authorization Endpoint |
| *OAuth20AuthorizationApi* | [**exchangeToken**](#exchangetoken) | **POST** /oauth2/token | OAuth2 Token Endpoint |
| *OAuth20AuthorizationApi* | [**submitConsent**](#submitconsent) | **POST** /oauth2/authorize | OAuth2 consent endpoint |
| *OAuth20ClientManagementApi* | [**changeActivation**](#changeactivation) | **PATCH** /api/2.0/clients/{clientId}/activation | Change client activation status |
| *OAuth20ClientManagementApi* | [**createClient**](#createclient) | **POST** /api/2.0/clients | Create a new OAuth2 client |
| *OAuth20ClientManagementApi* | [**deleteClient**](#deleteclient) | **DELETE** /api/2.0/clients/{clientId} | Delete an OAuth2 client |
| *OAuth20ClientManagementApi* | [**deleteTenantClients**](#deletetenantclients) | **DELETE** /api/2.0/clients/tenant | Delete all tenant OAuth2 clients |
| *OAuth20ClientManagementApi* | [**deleteUserClients**](#deleteuserclients) | **DELETE** /api/2.0/clients | Delete all user OAuth2 clients |
| *OAuth20ClientManagementApi* | [**regenerateSecret**](#regeneratesecret) | **PATCH** /api/2.0/clients/{clientId}/regenerate | Regenerate client secret |
| *OAuth20ClientManagementApi* | [**revokeUserClient**](#revokeuserclient) | **DELETE** /api/2.0/clients/{clientId}/revoke | Revoke client consent |
| *OAuth20ClientManagementApi* | [**updateClient**](#updateclient) | **PUT** /api/2.0/clients/{clientId} | Update an existing OAuth2 client |
| *OAuth20ClientQueryingApi* | [**getClient**](#getclient) | **GET** /api/2.0/clients/{clientId} | Get client details |
| *OAuth20ClientQueryingApi* | [**getClientInfo**](#getclientinfo) | **GET** /api/2.0/clients/{clientId}/info | Retrieves detailed information for a specific client |
| *OAuth20ClientQueryingApi* | [**getClients**](#getclients) | **GET** /api/2.0/clients | List clients |
| *OAuth20ClientQueryingApi* | [**getClientsInfo**](#getclientsinfo) | **GET** /api/2.0/clients/info | Retrieves a pageable list of client information |
| *OAuth20ClientQueryingApi* | [**getConsents**](#getconsents) | **GET** /api/2.0/clients/consents | Retrieves a pageable list of consents |
| *OAuth20ClientQueryingApi* | [**getPublicClientInfo**](#getpublicclientinfo) | **GET** /api/2.0/clients/{clientId}/public/info | Handles the GET request for public client information |
| *OAuth20DiscoveryApi* | [**handleOptions**](#handleoptions) | **OPTIONS** /.well-known/oauth-authorization-server |  |
| *OAuth20ScopeManagementApi* | [**getScopes**](#getscopes) | **GET** /api/2.0/scopes | List available OAuth2 scopes |



## OAuth20AuthorizationApi

### authorizeOAuth

> authorizeOAuth(response\_type, client\_id, redirect\_uri, scope)

`GET /oauth2/authorize`

OAuth2 Authorization Endpoint

Initiates the OAuth2 authorization flow.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **response\_type** | query | **String** | The OAuth 2.0 response type, must be &#39;code&#39; for authorization code flow. | [required] [example: code] |
| **client\_id** | query | **String** | The client identifier issued to the client during registration. | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] |
| **redirect\_uri** | query | **String** | The URL to redirect to after authorization is complete. | [required] [example: https://example.com] |
| **scope** | query | **String** | The space-separated list of requested scope permissions. | [required] [example: files:read] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authorization page | - | - |
| **400** | Invalid request parameters | - | - |

#### Return type

null (empty response body)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### exchangeToken

> exchangeToken_200_response exchangeToken(grant\_type, code, redirect\_uri, client\_id, client\_secret)

`POST /oauth2/token`

OAuth2 Token Endpoint

Exchange authorization code for access token

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **grant\_type** | form | **String** | The OAuth2 grant type, must be &#39;authorization_code&#39; for the authorization code flow. | [optional] |
| **code** | form | **String** | A temporary authorization code that is sent to the client to be exchanged for a token. | [optional] |
| **redirect\_uri** | form | **String** | The URL where the user will be redirected after successful or unsuccessful authentication. | [optional] |
| **client\_id** | form | **String** | The client identifier issued to the client during registration. | [optional] |
| **client\_secret** | form | **String** | The client secret issued to the client during registration. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully exchanged authorization code for access token | [**exchangeToken_200_response**](#model-exchangetoken-200-response) | - |
| **400** | Invalid request parameters | - | - |

#### Return type

[**exchangeToken_200_response**](#model-exchangetoken-200-response)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/x-www-form-urlencoded
- **Accept**: application/json

### submitConsent

> submitConsent(client\_id, state, scope)

`POST /oauth2/authorize`

OAuth2 consent endpoint

Sends consent approval

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **client\_id** | form | **String** | The client identifier issued to the client during registration. | [optional] |
| **state** | form | **String** | The random string used to solve the CSRF vulnerability problem. | [optional] |
| **scope** | form | **String** | The space-separated list of requested scope permissions. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **302** | Redirect to the client&#39;s redirect URI with authorization code | - | - |
| **400** | Invalid request parameters | - | - |

#### Return type

null (empty response body)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: Not defined

## OAuth20ClientManagementApi

### changeActivation

> Object changeActivation(clientId, ChangeClientActivationRequest)

`PATCH /api/2.0/clients/{clientId}/activation`

Change client activation status

Activates or deactivates an OAuth2 client. When deactivated, the client cannot request new access tokens, but existing tokens will remain valid until they expire.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to change activation for | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |
| **ChangeClientActivationRequest** | body | [**ChangeClientActivationRequest**](#model-changeclientactivationrequest) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client activation status successfully changed | **Object** | - |
| **400** | Invalid client ID format or activation status | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to change client activation | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **415** | Unsupported media type | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createClient

> ClientResponse createClient(CreateClientRequest)

`POST /api/2.0/clients`

Create a new OAuth2 client

Creates a new OAuth2 client with the specified configuration. The client will be created with the provided scopes, redirect URIs, and other settings. Returns the created client details including the generated client ID.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateClientRequest** | body | [**CreateClientRequest**](#model-createclientrequest) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **201** | Client successfully created | [**ClientResponse**](#model-clientresponse) | - |
| **400** | Invalid request - missing required fields or validation failed | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to create client | [**ProblemDetail**](#model-problemdetail) | - |
| **415** | Unsupported media type | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

[**ClientResponse**](#model-clientresponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteClient

> Object deleteClient(clientId)

`DELETE /api/2.0/clients/{clientId}`

Delete an OAuth2 client

Permanently deletes an OAuth2 client and all associated data. This will invalidate all access tokens and refresh tokens issued to this client. This operation cannot be undone.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to delete | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client successfully deleted | **Object** | - |
| **400** | Invalid client ID format | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to delete client | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteTenantClients

> Object deleteTenantClients()

`DELETE /api/2.0/clients/tenant`

Delete all tenant OAuth2 clients

Permanently deletes tenant OAuth2 clients and all associated data. This will invalidate all access tokens and refresh tokens issued to this client. This operation cannot be undone.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client successfully deleted | **Object** | - |
| **403** | Insufficient permissions to delete tenant clients | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteUserClients

> Object deleteUserClients()

`DELETE /api/2.0/clients`

Delete all user OAuth2 clients

Permanently deletes user OAuth2 clients and all associated data. This will invalidate all access tokens and refresh tokens issued to this client. This operation cannot be undone.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client successfully deleted | **Object** | - |
| **403** | Insufficient permissions to delete user clients | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### regenerateSecret

> ClientSecretResponse regenerateSecret(clientId)

`PATCH /api/2.0/clients/{clientId}/regenerate`

Regenerate client secret

Generates a new client secret for the specified OAuth2 client. The old secret will be immediately invalidated. This operation should be used with caution as it requires updating the secret in all client applications.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to regenerate secret for | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client secret successfully regenerated | [**ClientSecretResponse**](#model-clientsecretresponse) | - |
| **400** | Invalid client ID format | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to regenerate client secret | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

[**ClientSecretResponse**](#model-clientsecretresponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### revokeUserClient

> Object revokeUserClient(clientId)

`DELETE /api/2.0/clients/{clientId}/revoke`

Revoke client consent

Revokes all user consents for the specified OAuth2 client. This will invalidate all access tokens and refresh tokens issued to this client for the current user. The user will need to re-authorize the client to access their resources.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to revoke consent for | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client consent successfully revoked | **Object** | - |
| **400** | Invalid client ID format | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to revoke consent | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |
| **503** | Authorization service unavailable | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateClient

> Object updateClient(clientId, UpdateClientRequest)

`PUT /api/2.0/clients/{clientId}`

Update an existing OAuth2 client

Updates the configuration of an existing OAuth2 client. Allows modification of client name, description, redirect URIs, and other settings. The client ID cannot be modified.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to update | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |
| **UpdateClientRequest** | body | [**UpdateClientRequest**](#model-updateclientrequest) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client successfully updated | **Object** | - |
| **400** | Invalid request - missing required fields or validation failed | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to update client | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **415** | Unsupported media type | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

**Object**

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## OAuth20ClientQueryingApi

### getClient

> ClientResponse getClient(clientId)

`GET /api/2.0/clients/{clientId}`

Get client details

Retrieves detailed information about a specific OAuth2 client including its name, description, redirect URIs, and scopes.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to retrieve | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client details successfully retrieved | [**ClientResponse**](#model-clientresponse) | - |
| **400** | Invalid client ID format | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to view client | [**ProblemDetail**](#model-problemdetail) | - |
| **404** | Client not found | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

[**ClientResponse**](#model-clientresponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getClientInfo

> ClientInfoResponse getClientInfo(clientId)

`GET /api/2.0/clients/{clientId}/info`

Retrieves detailed information for a specific client

Retrieves the detailed information for a client with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to retrieve | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully retrieved client info | [**ClientInfoResponse**](#model-clientinforesponse) | - |
| **400** | Bad request | - | - |
| **429** | Too many requests | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error | - | - |

#### Return type

[**ClientInfoResponse**](#model-clientinforesponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getClients

> PageableResponse getClients(limit, last\_client\_id, last\_created\_on)

`GET /api/2.0/clients`

List clients

Retrieves a paginated list of OAuth2 clients. The results can be paginated using the limit parameter and last seen client ID/creation date.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **limit** | query | **Integer** (int32) | Pagination limit | [required] [example: 1] [default to 30] [min: 1] [max: 50] |
| **last\_client\_id** | query | **String** | ID of the last retrieved client | [optional] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] |
| **last\_created\_on** | query | **Date** (date-time) | Date of the last retrieved client | [optional] [example: 2024-04-04T12:00:00Z] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Client list successfully retrieved | [**PageableResponse**](#model-pageableresponse) | - |
| **400** | Invalid pagination parameters | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to list clients | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

[**PageableResponse**](#model-pageableresponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getClientsInfo

> PageableResponseClientInfoResponse getClientsInfo(limit, last\_client\_id, last\_created\_on)

`GET /api/2.0/clients/info`

Retrieves a pageable list of client information

Retrieves a paginated list of information for all clients.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **limit** | query | **Integer** (int32) | Pagination limit | [required] [example: 1] [min: 1] [max: 50] |
| **last\_client\_id** | query | **String** | ID of the last retrieved client | [optional] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] |
| **last\_created\_on** | query | **Date** (date-time) | Date of the last retrieved client | [optional] [example: 2024-04-04T12:00:00Z] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully retrieved clients info | [**PageableResponseClientInfoResponse**](#model-pageableresponseclientinforesponse) | - |
| **400** | Bad request | - | - |
| **429** | Too many requests | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error | - | - |

#### Return type

[**PageableResponseClientInfoResponse**](#model-pageableresponseclientinforesponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getConsents

> PageableModificationResponse getConsents(limit, last\_modified\_on)

`GET /api/2.0/clients/consents`

Retrieves a pageable list of consents

Retrieves a paginated list of user consents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **limit** | query | **Integer** (int32) | Pagination limit | [required] [example: 1] [min: 1] [max: 50] |
| **last\_modified\_on** | query | **Date** (date-time) | Date of the last retrieved consent | [optional] [example: 2024-04-04T12:00:00Z] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully retrieved user consents | [**PageableModificationResponse**](#model-pageablemodificationresponse) | - |

#### Return type

[**PageableModificationResponse**](#model-pageablemodificationresponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPublicClientInfo

> ClientInfoResponse getPublicClientInfo(clientId)

`GET /api/2.0/clients/{clientId}/public/info`

Handles the GET request for public client information

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **clientId** | path | **String** | ID of the client to retrieve | [required] [example: 6c7cf17b-1bd3-47d5-94c6-be2d3570e168] [minLength: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully retrieved client public info | [**ClientInfoResponse**](#model-clientinforesponse) | - |
| **400** | Bad request | - | - |
| **429** | Too many requests | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error | - | - |

#### Return type

[**ClientInfoResponse**](#model-clientinforesponse)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## OAuth20DiscoveryApi

### handleOptions

> Object handleOptions()

`OPTIONS /.well-known/oauth-authorization-server`



#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | **Object** | - |

#### Return type

**Object**

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: */*

## OAuth20ScopeManagementApi

### getScopes

> ScopeResponse getScopes()

`GET /api/2.0/scopes`

List available OAuth2 scopes

Retrieves a list of all available OAuth2 scopes for the specified tenant. The scopes define the permissions that can be requested by OAuth2 clients. The list is ordered alphabetically, with the &#39;openid&#39; scope always appearing first.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Scopes successfully retrieved | [**ScopeResponse**](#model-scoperesponse) | - |
| **400** | Invalid request parameters | [**ProblemDetail**](#model-problemdetail) | - |
| **403** | Insufficient permissions to list scopes | [**ProblemDetail**](#model-problemdetail) | - |
| **429** | Too many requests - rate limit exceeded | [**ProblemDetail**](#model-problemdetail) | - |
| **500** | Internal server error occurred | [**ProblemDetail**](#model-problemdetail) | - |

#### Return type

[**ScopeResponse**](#model-scoperesponse)

#### Authorization

[x-signature](#x-signature)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json


## Models


### Model ChangeClientActivationRequest
Client activation change request

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **status** | **Boolean** | The activation status of the client | [required] [example: true] |


### Model ClientInfoResponse
The response containing public client information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The client name. | [optional] |
| **description** | **String** | The client description. | [optional] |
| **scopes** | **Set** | The client scopes. | [optional] |
| **public** | **Boolean** |  | [optional] |
| **client\_id** | **String** | The client ID. | [optional] |
| **website\_url** | **String** | The URL to the client&#39;s website | [optional] |
| **terms\_url** | **String** | The URL to the client&#39;s terms of service. | [optional] |
| **policy\_url** | **String** | The URL to the client&#39;s privacy policy. | [optional] |
| **logo** | **String** | The client logo in base64 format. | [optional] |
| **authentication\_methods** | **Set** | The authentication methods supported by the client. | [optional] |
| **is\_public** | **Boolean** | Indicates whether the client is accessible by third-party tenants. | [optional] |
| **created\_on** | **Date** (date-time) | The date and time when the client was created. | [optional] |
| **created\_by** | **String** | The user who created the client. | [optional] |
| **modified\_on** | **Date** (date-time) | The date and time when the client was last modified. | [optional] |
| **modified\_by** | **String** | The user who last modified the client. | [optional] |


### Model ClientResponse

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The client name. | [optional] |
| **description** | **String** | The client description. | [optional] |
| **tenant** | **Long** (int64) | The tenant ID associated with the client. | [optional] |
| **scopes** | **Set** | The client scopes. | [optional] |
| **enabled** | **Boolean** | Specifies if the client is currently enabled or not. | [optional] |
| **client\_id** | **String** | The client identifier issued to the client during registration. | [optional] |
| **client\_secret** | **String** | The client secret issued to the client during registration. | [optional] |
| **website\_url** | **String** | The URL to the client&#39;s website. | [optional] |
| **terms\_url** | **String** | The URL to the client&#39;s terms of service. | [optional] |
| **policy\_url** | **String** | The URL to the client&#39;s privacy policy. | [optional] |
| **logo** | **String** | The URL to the client&#39;s logo. | [optional] |
| **authentication\_methods** | **Set** | The authentication methods supported by the client. | [optional] |
| **redirect\_uris** | **Set** | The list of allowed redirect URIs. | [optional] |
| **allowed\_origins** | **Set** | The list of allowed CORS origins. | [optional] |
| **logout\_redirect\_uris** | **Set** | The list of allowed logout redirect URIs. | [optional] |
| **created\_on** | **Date** (date-time) | The date and time when the client was created. | [optional] |
| **created\_by** | **String** | The user who created the client. | [optional] |
| **modified\_on** | **Date** (date-time) | The date and time when the client was last modified. | [optional] |
| **modified\_by** | **String** | The user who last modified the client. | [optional] |
| **is\_public** | **Boolean** | Indicates whether the client is accessible by third-party tenants. | [optional] |


### Model ClientSecretResponse
The response containing the regenerated client secret.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **client\_secret** | **String** | The newly generated client secret. | [optional] |


### Model CreateClientRequest
Client creation request containing client details

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The client name. | [optional] [example: Example Client] [minLength: 3] [maxLength: 256] |
| **description** | **String** | The description of the client | [optional] [example: Description of the client] [minLength: 0] [maxLength: 255] |
| **logo** | **String** | The logo of the client in base64 format | [optional] [example: data:image/png;base64,...] [minLength: 1] [pattern: /^data:image\/(?:png\|jpeg\|jpg\|svg\\+xml);base64,.*.{1,}/] |
| **scopes** | **Set** | The scopes for the client | [optional] [example: [read, write]] |
| **public** | **Boolean** |  | [optional] |
| **allow\_pkce** | **Boolean** | Indicates whether PKCE is allowed for the client | [optional] [example: true] |
| **is\_public** | **Boolean** | Indicates if the client is public | [optional] [example: false] |
| **website\_url** | **String** | The website URL of the client | [optional] [example: http://example.com] [minLength: 1] [pattern: /^(https?:\/\/)?([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$\|^https?:\/\/(\\d{1,3}\\.){3}\\d{1,3}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$/] |
| **terms\_url** | **String** | The terms URL of the client | [optional] [example: http://example.com/terms] [minLength: 1] [pattern: /^(https?:\/\/)?([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$\|^https?:\/\/(\\d{1,3}\\.){3}\\d{1,3}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$/] |
| **policy\_url** | **String** | The policy URL of the client | [optional] [example: http://example.com/policy] [minLength: 1] [pattern: /^(https?:\/\/)?([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$\|^https?:\/\/(\\d{1,3}\\.){3}\\d{1,3}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$/] |
| **redirect\_uris** | **Set** | The redirect URIs for the client | [required] [example: [http://example.com/redirect]] |
| **allowed\_origins** | **Set** | The allowed origins for the client | [required] [example: [http://example.com]] |
| **logout\_redirect\_uri** | **String** | The logout redirect URI for the client | [optional] [example: http://example.com/logout] [minLength: 1] [pattern: /^(https?:\/\/)?([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$\|^https?:\/\/(\\d{1,3}\\.){3}\\d{1,3}(:\\d+)?(\/[a-zA-Z0-9-._~:\/?#\\[\\]@!$&'()*+,;=]*)?$/] |


### Model PageableModificationResponse
The response containing paginated modification information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **data** | **Object** | The paginated modification data. | [optional] |
| **limit** | **Integer** (int32) | The maximum number of results returned per page. | [optional] |
| **last\_modified\_on** | **Date** (date-time) | The date when the user consent was last modified. | [optional] |


### Model PageableResponse
The response containing paginated data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **data** | **Object** | The paginated data. | [optional] |
| **limit** | **Integer** (int32) | The maximum number of results returned per page. | [optional] |
| **last\_client\_id** | **String** | The identifier of the last retrieved client. | [optional] |
| **last\_created\_on** | **Date** (date-time) | The creation date of the last retrieved client. | [optional] |


### Model PageableResponseClientInfoResponse
The response containing paginated client information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **data** | **Object** | The paginated client data. | [optional] |
| **limit** | **Integer** (int32) | The maximum number of results returned per page. | [optional] |
| **last\_client\_id** | **String** | The identifier of the last retrieved client. | [optional] |
| **last\_created\_on** | **Date** (date-time) | The creation date of the last retrieved client. | [optional] |


### Model ProblemDetail

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | **URI** (uri) |  | [optional] |
| **title** | **String** |  | [optional] |
| **status** | **Integer** (int32) |  | [optional] |
| **detail** | **String** |  | [optional] |
| **instance** | **URI** (uri) |  | [optional] |
| **properties** | **Map** |  | [optional] |


### Model ScopeResponse
The response containing the scope information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The scope name. | [optional] |
| **group** | **String** | The group the scope belongs to. | [optional] |
| **type** | **String** | The scope type. | [optional] |


### Model UpdateClientRequest
Client update request containing modified client details

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The name of the client | [optional] [example: Updated Client] [minLength: 1] |
| **description** | **String** | The description of the client | [optional] [example: Updated description of the client] [minLength: 0] [maxLength: 255] |
| **logo** | **String** | The logo of the client in base64 format | [optional] [example: data:image/png;base64,...] [minLength: 1] [pattern: /^data:image\/(?:png\|jpeg\|jpg\|svg\\+xml);base64,.*.{1,}/] |
| **public** | **Boolean** |  | [optional] |
| **allow\_pkce** | **Boolean** | Indicates whether PKCE is allowed for the client | [optional] [example: true] |
| **is\_public** | **Boolean** | Indicates whether client is accessible by third-party tenants | [optional] [example: false] |
| **allowed\_origins** | **Set** | The allowed origins for the client | [optional] [example: [http://allowed.origin]] |


### Model exchangeToken 200 response

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **access\_token** | **String** | The access token issued by the authorization server. | [optional] [example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...] |
| **token\_type** | **String** | The type of token issued, typically &#39;Bearer&#39;. | [optional] [example: Bearer] |
| **expires\_in** | **Integer** | The number of seconds until the access token expires. | [optional] [example: 3600] |
| **refresh\_token** | **String** | The token used to obtain a new access token when the current one expires. | [optional] [example: def502...] |


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

