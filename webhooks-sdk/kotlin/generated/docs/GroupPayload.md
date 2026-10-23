
# GroupPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**java.util.UUID**](java.util.UUID.md) | The group ID. |  [optional] |
| **name** | **kotlin.String** | The group name. |  [optional] |
| **parent** | [**java.util.UUID**](java.util.UUID.md) | The parent group ID; absent for a top-level group. |  [optional] |
| **category** | [**java.util.UUID**](java.util.UUID.md) | The group category ID. |  [optional] |
| **isLDAP** | **kotlin.Boolean** | Specifies whether the group comes from LDAP or not. |  [optional] |
| **isSystem** | **kotlin.Boolean** | Specifies whether the group is a system group or not. |  [optional] |
| **manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **membersCount** | **kotlin.Int** | The number of group members. |  [optional] |



