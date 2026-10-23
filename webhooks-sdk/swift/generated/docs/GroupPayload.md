# GroupPayload

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** | The group ID. | [optional] 
**name** | **String** | The group name. | [optional] 
**parent** | **UUID** | The parent group ID; absent for a top-level group. | [optional] 
**category** | **UUID** | The group category ID. | [optional] 
**isLDAP** | **Bool** | Specifies whether the group comes from LDAP or not. | [optional] 
**isSystem** | **Bool** | Specifies whether the group is a system group or not. | [optional] 
**manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**membersCount** | **Int** | The number of group members. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


