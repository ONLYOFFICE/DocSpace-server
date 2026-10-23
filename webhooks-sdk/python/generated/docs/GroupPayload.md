# GroupPayload

The group carried by a group.* webhook.  A copy of GroupDto, deliberately not derived from it. The member list is not carried: a group can hold thousands of users and every one of them would be expanded into the body of every group event. Receivers that need the roster should call GET api/2.0/group/{id} with MembersCount as the hint that it changed.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** | The group ID. | [optional] 
**name** | **str** | The group name. | [optional] 
**parent** | **UUID** | The parent group ID; absent for a top-level group. | [optional] 
**category** | **UUID** | The group category ID. | [optional] 
**is_ldap** | **bool** | Specifies whether the group comes from LDAP or not. | [optional] 
**is_system** | **bool** | Specifies whether the group is a system group or not. | [optional] 
**manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**members_count** | **int** | The number of group members. | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.group_payload import GroupPayload

# TODO update the JSON string below
json = "{}"
# create an instance of GroupPayload from a JSON string
group_payload_instance = GroupPayload.from_json(json)
# print the JSON string representation of the object
print(GroupPayload.to_json())

# convert the object into a dict
group_payload_dict = group_payload_instance.to_dict()
# create an instance of GroupPayload from a dict
group_payload_from_dict = GroupPayload.from_dict(group_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


