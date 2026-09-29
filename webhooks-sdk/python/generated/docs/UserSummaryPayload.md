# UserSummaryPayload

A user referenced from another payload.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**display_name** | **str** |  | [optional] 
**user_name** | **str** |  | [optional] 
**email** | **str** |  | [optional] 
**avatar** | **str** |  | [optional] 
**profile_url** | **str** |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.user_summary_payload import UserSummaryPayload

# TODO update the JSON string below
json = "{}"
# create an instance of UserSummaryPayload from a JSON string
user_summary_payload_instance = UserSummaryPayload.from_json(json)
# print the JSON string representation of the object
print(UserSummaryPayload.to_json())

# convert the object into a dict
user_summary_payload_dict = user_summary_payload_instance.to_dict()
# create an instance of UserSummaryPayload from a dict
user_summary_payload_from_dict = UserSummaryPayload.from_dict(user_summary_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


