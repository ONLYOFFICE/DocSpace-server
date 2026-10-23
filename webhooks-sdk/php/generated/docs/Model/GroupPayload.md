# GroupPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **string** | The group ID. | [optional]
**name** | **string** | The group name. | [optional]
**parent** | **string** | The parent group ID; absent for a top-level group. | [optional]
**category** | **string** | The group category ID. | [optional]
**is_ldap** | **bool** | Specifies whether the group comes from LDAP or not. | [optional]
**is_system** | **bool** | Specifies whether the group is a system group or not. | [optional]
**manager** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserSummaryPayload**](UserSummaryPayload.md) |  | [optional]
**members_count** | **int** | The number of group members. | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
