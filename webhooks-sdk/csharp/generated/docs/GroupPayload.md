# DocSpace.Webhooks.SDK.Model.GroupPayload
The group carried by a group.* webhook.  A copy of GroupDto, deliberately not derived from it. The member list is not carried: a group can hold thousands of users and every one of them would be expanded into the body of every group event. Receivers that need the roster should call GET api/2.0/group/{id} with MembersCount as the hint that it changed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | The group ID. | [optional] 
**Name** | **string** | The group name. | [optional] 
**Parent** | **Guid** | The parent group ID; absent for a top-level group. | [optional] 
**Category** | **Guid** | The group category ID. | [optional] 
**IsLDAP** | **bool** | Specifies whether the group comes from LDAP or not. | [optional] 
**IsSystem** | **bool** | Specifies whether the group is a system group or not. | [optional] 
**Manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**MembersCount** | **int** | The number of group members. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

