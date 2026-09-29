# GroupPayload

ASC.Api.Core/Webhook/Payloads/GroupWebhookDto.cs. A copy of the REST GroupDto.  The member list is not carried - a group can hold thousands of users and each would be expanded into every group event. `membersCount` is the hint that the roster changed; read it from GET api/2.0/group/{id}. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**name** | **str** |  | [optional] 
**parent** | **UUID** |  | [optional] 
**category** | **UUID** |  | [optional] 
**is_ldap** | **bool** |  | [optional] 
**is_system** | **bool** |  | [optional] 
**manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**members_count** | **int** |  | [optional] 

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


