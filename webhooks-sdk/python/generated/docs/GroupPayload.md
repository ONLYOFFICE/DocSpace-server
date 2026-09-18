# GroupPayload

ASC.Core.Common/Core/GroupInfo.cs. Note `ID` in C#; the camelCase policy lowercases the whole leading run, so it is \"id\" on the wire, while `CategoryID` becomes \"categoryID\". 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**name** | **str** |  | [optional] 
**category_id** | **UUID** |  | [optional] 
**parent** | [**GroupPayload**](GroupPayload.md) |  | [optional] 
**sid** | **str** | LDAP identifier. REVIEW. | [optional] 
**removed** | **bool** |  | [optional] 

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


