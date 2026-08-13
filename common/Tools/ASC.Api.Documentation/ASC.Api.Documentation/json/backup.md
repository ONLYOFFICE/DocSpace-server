# ONLYOFFICE DocSpace Backup API

The browsable version of this reference, with a request builder and code samples, is published at
<https://api.onlyoffice.com/docspace/api-backend/usage-api/>.

All URIs are relative to *https://yourportal.onlyoffice.com*, where the host is the address of your DocSpace instance.

## Endpoints

| Class | Method | HTTP request | Description |
|------------ | ------------- | ------------- | -------------|
| *BackupApi* | [**cancelBackup**](#cancelbackup) | **POST** /api/2.0/backup/cancelbackup | Cancel current backup |
| *BackupApi* | [**createBackupSchedule**](#createbackupschedule) | **POST** /api/2.0/backup/createbackupschedule | Create the backup schedule |
| *BackupApi* | [**deleteBackup**](#deletebackup) | **DELETE** /api/2.0/backup/deletebackup/{id} | Delete the backup |
| *BackupApi* | [**deleteBackupHistory**](#deletebackuphistory) | **DELETE** /api/2.0/backup/deletebackuphistory | Delete the backup history |
| *BackupApi* | [**deleteBackupSchedule**](#deletebackupschedule) | **DELETE** /api/2.0/backup/deletebackupschedule | Delete the backup schedule |
| *BackupApi* | [**getBackupHistory**](#getbackuphistory) | **GET** /api/2.0/backup/getbackuphistory | Get the backup history |
| *BackupApi* | [**getBackupProgress**](#getbackupprogress) | **GET** /api/2.0/backup/getbackupprogress | Get the backup progress |
| *BackupApi* | [**getBackupSchedule**](#getbackupschedule) | **GET** /api/2.0/backup/getbackupschedule | Get the backup schedule |
| *BackupApi* | [**getBackupsCount**](#getbackupscount) | **GET** /api/2.0/backup/getbackupscount | Get the number of backups |
| *BackupApi* | [**getBackupsCounts**](#getbackupscounts) | **GET** /api/2.0/backup/getbackupscountbypaid | Get the number of free and paid backups |
| *BackupApi* | [**getBackupsServiceState**](#getbackupsservicestate) | **GET** /api/2.0/backup/getservicestate | Get the backup service state |
| *BackupApi* | [**getRestoreProgress**](#getrestoreprogress) | **GET** /api/2.0/backup/getrestoreprogress | Get the restoring progress |
| *BackupApi* | [**startBackup**](#startbackup) | **POST** /api/2.0/backup/startbackup | Start the backup |
| *BackupApi* | [**startBackupRestore**](#startbackuprestore) | **POST** /api/2.0/backup/startrestore | Start the restoring process |



## BackupApi

### cancelBackup

> BooleanWrapper cancelBackup()

`POST /api/2.0/backup/cancelbackup`

Cancel current backup

Cancel current backup.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
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

### createBackupSchedule

> BooleanWrapper createBackupSchedule(BackupScheduleDto)

`POST /api/2.0/backup/createbackupschedule`

Create the backup schedule

Creates the backup schedule of the current portal with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BackupScheduleDto** | body | [**BackupScheduleDto**](#model-backupscheduledto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | BackupStored must be 1 - 30 or backup can not start as dump | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | Access denied | - | - |
| **404** | The required folder was not found | - | - |
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

### deleteBackup

> BooleanWrapper deleteBackup(id)

`DELETE /api/2.0/backup/deletebackup/{id}`

Delete the backup

Deletes the backup with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The backup ID. | [required] [example: "00000000-0000-0000-0000-000000000000"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
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

### deleteBackupHistory

> BooleanWrapper deleteBackupHistory(Dump)

`DELETE /api/2.0/backup/deletebackuphistory`

Delete the backup history

Deletes the backup history from the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
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

### deleteBackupSchedule

> BooleanWrapper deleteBackupSchedule(Dump)

`DELETE /api/2.0/backup/deletebackupschedule`

Delete the backup schedule

Deletes the backup schedule of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
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

### getBackupHistory

> BackupHistoryRecordArrayWrapper getBackupHistory(Dump)

`GET /api/2.0/backup/getbackuphistory`

Get the backup history

Returns the history of the started backup.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of backup history records | [**BackupHistoryRecordArrayWrapper**](#model-backuphistoryrecordarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupHistoryRecordArrayWrapper**](#model-backuphistoryrecordarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupProgress

> BackupProgressWrapper getBackupProgress(Dump)

`GET /api/2.0/backup/getbackupprogress`

Get the backup progress

Returns the progress of the started backup.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link | [**BackupProgressWrapper**](#model-backupprogresswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupProgressWrapper**](#model-backupprogresswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupSchedule

> ScheduleWrapper getBackupSchedule(Dump)

`GET /api/2.0/backup/getbackupschedule`

Get the backup schedule

Returns the backup schedule of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup schedule | [**ScheduleWrapper**](#model-schedulewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ScheduleWrapper**](#model-schedulewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupsCount

> Int32Wrapper getBackupsCount(from, to, paid)

`GET /api/2.0/backup/getbackupscount`

Get the number of backups

Returns the number of backups for a period of time. The default is the current calendar month.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **from** | query | **Date** (date-time) | The from date. | [optional] [example: "2025-01-01T00:00:00Z"] |
| **to** | query | **Date** (date-time) | The to date. | [optional] [example: "2025-12-31T23:59:59Z"] |
| **paid** | query | **Boolean** | Specifies if the backups are paid or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Number of backups | [**Int32Wrapper**](#model-int32wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | From date must be less than to date | - | - |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**Int32Wrapper**](#model-int32wrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupsCounts

> BackupsCountResultWrapper getBackupsCounts(from, to, paid)

`GET /api/2.0/backup/getbackupscountbypaid`

Get the number of free and paid backups

Returns the number of free and paid backups for a period of time. The default is the current calendar month.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **from** | query | **Date** (date-time) | The from date. | [optional] [example: 2025-01-01T00:00:00Z] |
| **to** | query | **Date** (date-time) | The to date. | [optional] [example: 2025-12-31T23:59:59Z] |
| **paid** | query | **Boolean** | Specifies if the backups are paid or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Number of free and paid backups | [**BackupsCountResultWrapper**](#model-backupscountresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | From date must be less than to date | - | - |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupsCountResultWrapper**](#model-backupscountresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupsServiceState

> BackupServiceStateWrapper getBackupsServiceState()

`GET /api/2.0/backup/getservicestate`

Get the backup service state

Returns the backup service state.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup service state | [**BackupServiceStateWrapper**](#model-backupservicestatewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupServiceStateWrapper**](#model-backupservicestatewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRestoreProgress

> BackupProgressWrapper getRestoreProgress(Dump)

`GET /api/2.0/backup/getrestoreprogress`

Get the restoring progress

Returns the progress of the started restoring process.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link | [**BackupProgressWrapper**](#model-backupprogresswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupProgressWrapper**](#model-backupprogresswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startBackup

> BackupProgressWrapper startBackup(BackupDto)

`POST /api/2.0/backup/startbackup`

Start the backup

Starts the backup of the current portal with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BackupDto** | body | [**BackupDto**](#model-backupdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link | [**BackupProgressWrapper**](#model-backupprogresswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Wrong folder type or backup can&#x60;t start as dump | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | Access denied | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupProgressWrapper**](#model-backupprogresswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startBackupRestore

> BackupProgressWrapper startBackupRestore(BackupRestoreDto)

`POST /api/2.0/backup/startrestore`

Start the restoring process

Starts the data restoring process of the current portal with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BackupRestoreDto** | body | [**BackupRestoreDto**](#model-backuprestoredto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link | [**BackupProgressWrapper**](#model-backupprogresswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Backup can not start as dump | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | Access denied | - | - |
| **404** | The required file or folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BackupProgressWrapper**](#model-backupprogresswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json


## Models


### Model BackupDto
The backup parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **storageType** | [**BackupStorageType**](#model-backupstoragetype) | The backup storage type. | [optional] [enum: 0, 1, 2, 3, 4, 5] |
| **storageParams** | [**List**](#model-itemkeyvaluepairobjectobject) | The backup storage parameters. | [optional] [example: [{key=path, value=/backup}]] [nullable] |
| **dump** | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: false] |


### Model BackupHistoryRecord
The backup history parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The backup ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **fileName** | **String** | The backup file name. | [required] [example: tenant-backup] [nullable] |
| **storageType** | [**BackupStorageType**](#model-backupstoragetype) | The backup storage type. | [required] [enum: 0, 1, 2, 3, 4, 5] |
| **createdOn** | **Date** (date-time) | The backup creation date. | [required] [example: 2026-03-01T02:15:00Z] |
| **expiresOn** | **Date** (date-time) | The backup expiration date. | [required] [example: 2026-03-31T02:15:00Z] |


### Model BackupHistoryRecordArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-backuphistoryrecord) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BackupHistoryRecordArrayWrapper.links item

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **href** | **String** | URL of the link | [optional] |
| **action** | **String** | Action associated with the link | [optional] |


### Model BackupPeriod
[0 - Every day, 1 - Every week, 2 - Every month]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BackupProgress
The backup progress parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isCompleted** | **Boolean** | Specifies if the backup is completed or not. | [optional] [example: false] |
| **progress** | **Integer** (int32) | The backup progress in percentage. | [optional] [example: 50] |
| **error** | **String** | The backup error message. | [optional] [nullable] |
| **warning** | **String** | The backup warning message. | [optional] [nullable] |
| **link** | **String** | The backup link. | [optional] [example: https://example.com/backup/task_123] [nullable] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **backupProgressEnum** | [**BackupProgressEnum**](#model-backupprogressenum) | The backup progress type. | [optional] [enum: 0, 1, 2] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) | The backup progress status. | [optional] [enum: 0, 1, 2, 3, 4] |
| **taskId** | **String** | The task ID. | [optional] [example: task_123] [nullable] |


### Model BackupProgressEnum
[0 - Backup, 1 - Restore, 2 - Transfer]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BackupProgressWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**BackupProgress**](#model-backupprogress) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BackupRestoreDto
The backup restoring parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **backupId** | **String** | The backup ID. | [required] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **storageType** | [**BackupStorageType**](#model-backupstoragetype) | The backup storage type. | [optional] [enum: 0, 1, 2, 3, 4, 5] |
| **storageParams** | [**List**](#model-itemkeyvaluepairobjectobject) | The backup storage parameters. | [optional] [example: [{key=path, value=/backup}]] [nullable] |
| **notify** | **Boolean** | Notifies users about the portal restoring process or not. | [optional] [example: true] |
| **dump** | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: false] |


### Model BackupScheduleDto
The backup schedule parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **storageType** | [**BackupStorageType**](#model-backupstoragetype) | The backup storage type. | [optional] [enum: 0, 1, 2, 3, 4, 5] |
| **storageParams** | [**List**](#model-itemkeyvaluepairobjectobject) | The backup storage parameters. | [optional] [example: [{key=path, value=/backup}]] [nullable] |
| **backupsStored** | **Integer** (int32) | The maximum number of the stored backup copies. | [optional] [example: 5] [nullable] |
| **cronParams** | [**Cron**](#model-cron) | The backup cron parameters. | [optional] |
| **dump** | **Boolean** | Specifies if a dump will be created or not. | [optional] [example: false] |


### Model BackupServiceStateDto
Backup service state.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies if the backup service is enabled or not. | [optional] [example: true] |


### Model BackupServiceStateWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**BackupServiceStateDto**](#model-backupservicestatedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BackupStorageType
[0 - Documents, 1 - Thridparty documents, 2 - Custom cloud, 3 - Local, 4 - Data store, 5 - Thirdparty consumer]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BackupsCountResultDto
The number of backups.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **free** | **Integer** (int32) | The number of free backups. | [optional] [example: 3] |
| **paid** | **Integer** (int32) | The number of paid backups. | [optional] [example: 5] |


### Model BackupsCountResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**BackupsCountResultDto**](#model-backupscountresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BooleanWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Boolean** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Cron
The backup cron parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **period** | [**BackupPeriod**](#model-backupperiod) | The backup period type. | [optional] [enum: 0, 1, 2] |
| **hour** | **Integer** (int32) | The time of the day to start the backup process. | [optional] [example: 0] |
| **day** | **Integer** (int32) | The day of the week to start the backup process. | [optional] [example: 0] [nullable] |


### Model CronParams
The backup cron parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **period** | [**BackupPeriod**](#model-backupperiod) | The backup period type. | [optional] [enum: 0, 1, 2] |
| **hour** | **Integer** (int32) | The time of the day to start the backup process. | [optional] [example: 0] |
| **day** | **Integer** (int32) | The day of the week to start the backup process. | [optional] [example: 0] |


### Model DistributedTaskStatus
[0 - Created, 1 - Running, 2 - Completed, 3 - Canceled, 4 - Failted]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Int32Wrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Integer** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ItemKeyValuePairObjectObject

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **null** |  | [optional] |
| **value** | **null** |  | [optional] |


### Model ScheduleDto
The backup schedule parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **storageType** | [**BackupStorageType**](#model-backupstoragetype) | The backup storage type. | [required] [enum: 0, 1, 2, 3, 4, 5] |
| **storageParams** | **null** | The backup storage parameters. | [required] [example: {}] |
| **cronParams** | [**CronParams**](#model-cronparams) | The backup cron parameters. | [required] |
| **backupsStored** | **Integer** (int32) | The maximum number of the stored backup copies. | [optional] [example: 5] [nullable] |
| **lastBackupTime** | **Date** (date-time) | The date and time when the last backup was reated. | [required] [example: 2026-01-01T00:00:00Z] |
| **dump** | **Boolean** | Specifies if a dump will be created or not. | [required] [example: false] |


### Model ScheduleWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ScheduleDto**](#model-scheduledto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-backuphistoryrecordarraywrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


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

