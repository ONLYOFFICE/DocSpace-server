

# GroupPayload

The group carried by a group.* webhook.  A copy of GroupDto, deliberately not derived from it. The member list is not carried: a group can hold thousands of users and every one of them would be expanded into the body of every group event. Receivers that need the roster should call GET api/2.0/group/{id} with MembersCount as the hint that it changed.

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **UUID** | The group ID. |  [optional] |
|**name** | **String** | The group name. |  [optional] |
|**parent** | **UUID** | The parent group ID; absent for a top-level group. |  [optional] |
|**category** | **UUID** | The ID of the group category: the empty GUID for the groups members create, and one fixed category ID for the built-in system groups. |  [optional] |
|**isLDAP** | **Boolean** | Specifies whether the group comes from LDAP or not. |  [optional] |
|**isSystem** | **Boolean** | Specifies whether the group is a system group or not. |  [optional] |
|**manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**membersCount** | **Integer** | The number of group members. |  [optional] |



