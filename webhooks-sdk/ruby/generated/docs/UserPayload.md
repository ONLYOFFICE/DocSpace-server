# DocspaceWebhooksSdk::UserPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** |  | [optional] |
| **first_name** | **String** |  | [optional] |
| **last_name** | **String** |  | [optional] |
| **user_name** | **String** |  | [optional] |
| **email** | **String** |  | [optional] |
| **birth_date** | **Time** |  | [optional] |
| **sex** | **Boolean** |  | [optional] |
| **status** | **Integer** | enum EmployeeStatus | [optional] |
| **activation_status** | **Integer** | enum EmployeeActivationStatus (flags) | [optional] |
| **terminated_date** | **Time** |  | [optional] |
| **title** | **String** |  | [optional] |
| **work_from_date** | **Time** |  | [optional] |
| **location** | **String** |  | [optional] |
| **notes** | **String** |  | [optional] |
| **contacts** | **String** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  | [optional] |
| **contacts_list** | **Array&lt;String&gt;** |  | [optional] |
| **removed** | **Boolean** |  | [optional] |
| **last_modified** | **Time** |  | [optional] |
| **tenant_id** | **Integer** |  | [optional] |
| **culture_name** | **String** |  | [optional] |
| **mobile_phone** | **String** |  | [optional] |
| **mobile_phone_activation_status** | **Integer** | enum MobilePhoneActivationStatus | [optional] |
| **create_date** | **Time** |  | [optional] |
| **created_by** | **String** |  | [optional] |
| **spam** | **Boolean** |  | [optional] |
| **sid** | **String** | LDAP identifier. REVIEW. | [optional] |
| **ldap_qouta** | **Integer** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  | [optional] |
| **sso_name_id** | **String** | SAML identifier. REVIEW. | [optional] |
| **sso_session_id** | **String** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  | [optional] |
| **is_active** | **Boolean** | computed getter | [optional][readonly] |
| **check_activation** | **Boolean** | computed getter | [optional][readonly] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::UserPayload.new(
  id: null,
  first_name: null,
  last_name: null,
  user_name: null,
  email: null,
  birth_date: null,
  sex: null,
  status: null,
  activation_status: null,
  terminated_date: null,
  title: null,
  work_from_date: null,
  location: null,
  notes: null,
  contacts: null,
  contacts_list: null,
  removed: null,
  last_modified: null,
  tenant_id: null,
  culture_name: null,
  mobile_phone: null,
  mobile_phone_activation_status: null,
  create_date: null,
  created_by: null,
  spam: null,
  sid: null,
  ldap_qouta: null,
  sso_name_id: null,
  sso_session_id: null,
  is_active: null,
  check_activation: null
)
```

