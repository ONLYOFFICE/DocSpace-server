# GroupSummaryPayload

A group referenced from another payload.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**name** | **str** |  | [optional] 
**manager** | **str** | The user name of the group manager, not an id. | [optional] 
**is_system** | **bool** |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.group_summary_payload import GroupSummaryPayload

# TODO update the JSON string below
json = "{}"
# create an instance of GroupSummaryPayload from a JSON string
group_summary_payload_instance = GroupSummaryPayload.from_json(json)
# print the JSON string representation of the object
print(GroupSummaryPayload.to_json())

# convert the object into a dict
group_summary_payload_dict = group_summary_payload_instance.to_dict()
# create an instance of GroupSummaryPayload from a dict
group_summary_payload_from_dict = GroupSummaryPayload.from_dict(group_summary_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


