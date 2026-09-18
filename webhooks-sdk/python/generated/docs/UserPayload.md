# UserPayload

ASC.Core.Common/Core/UserInfo.cs -- the domain entity, verbatim. It carries NO [JsonIgnore] at all, so every public member below reaches the wire. sid / ssoNameId / ssoSessionId / ldapQouta are null on portals without LDAP or SSO and therefore invisible in most test captures; they DO appear on LDAP/SSO tenants, and are flagged REVIEW below. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**first_name** | **str** |  | [optional] 
**last_name** | **str** |  | [optional] 
**user_name** | **str** |  | [optional] 
**email** | **str** |  | [optional] 
**birth_date** | **datetime** |  | [optional] 
**sex** | **bool** |  | [optional] 
**status** | **int** | enum EmployeeStatus | [optional] 
**activation_status** | **int** | enum EmployeeActivationStatus (flags) | [optional] 
**terminated_date** | **datetime** |  | [optional] 
**title** | **str** |  | [optional] 
**work_from_date** | **datetime** |  | [optional] 
**location** | **str** |  | [optional] 
**notes** | **str** |  | [optional] 
**contacts** | **str** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  | [optional] 
**contacts_list** | **List[str]** |  | [optional] 
**removed** | **bool** |  | [optional] 
**last_modified** | **datetime** |  | [optional] 
**tenant_id** | **int** |  | [optional] 
**culture_name** | **str** |  | [optional] 
**mobile_phone** | **str** |  | [optional] 
**mobile_phone_activation_status** | **int** | enum MobilePhoneActivationStatus | [optional] 
**create_date** | **datetime** |  | [optional] 
**created_by** | **UUID** |  | [optional] 
**spam** | **bool** |  | [optional] 
**sid** | **str** | LDAP identifier. REVIEW. | [optional] 
**ldap_qouta** | **int** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  | [optional] 
**sso_name_id** | **str** | SAML identifier. REVIEW. | [optional] 
**sso_session_id** | **str** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  | [optional] 
**is_active** | **bool** | computed getter | [optional] [readonly] 
**check_activation** | **bool** | computed getter | [optional] [readonly] 

## Example

```python
from docspace_webhooks_sdk.models.user_payload import UserPayload

# TODO update the JSON string below
json = "{}"
# create an instance of UserPayload from a JSON string
user_payload_instance = UserPayload.from_json(json)
# print the JSON string representation of the object
print(UserPayload.to_json())

# convert the object into a dict
user_payload_dict = user_payload_instance.to_dict()
# create an instance of UserPayload from a dict
user_payload_from_dict = UserPayload.from_dict(user_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


