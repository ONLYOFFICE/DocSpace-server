# UserSummaryPayload

A user referenced from another webhook payload.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** | The user ID. | [optional] 
**display_name** | **str** | The HTML-encoded user display name. | [optional] 
**user_name** | **str** | The user name. | [optional] 
**avatar** | **str** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] 
**profile_url** | **str** | The absolute link to the user in the portal&#39;s list of people, or of guests for a guest; today it is a search of that list by the user&#39;s email address. Absent when the subscription&#39;s creator is a User or a Guest, who may not see the contacts of other users, unless this user is that creator. | [optional] 

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


