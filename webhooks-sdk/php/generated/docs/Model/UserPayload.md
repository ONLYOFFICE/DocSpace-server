# UserPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **string** |  | [optional]
**first_name** | **string** |  | [optional]
**last_name** | **string** |  | [optional]
**user_name** | **string** |  | [optional]
**email** | **string** |  | [optional]
**birth_date** | **\DateTime** |  | [optional]
**sex** | **bool** |  | [optional]
**status** | **int** | enum EmployeeStatus | [optional]
**activation_status** | **int** | enum EmployeeActivationStatus (flags) | [optional]
**terminated_date** | **\DateTime** |  | [optional]
**title** | **string** |  | [optional]
**work_from_date** | **\DateTime** |  | [optional]
**location** | **string** |  | [optional]
**notes** | **string** |  | [optional]
**contacts** | **string** | Flattened form of contactsList. BOTH are emitted -- the same data twice. | [optional]
**contacts_list** | **string[]** |  | [optional]
**removed** | **bool** |  | [optional]
**last_modified** | **\DateTime** |  | [optional]
**tenant_id** | **int** |  | [optional]
**culture_name** | **string** |  | [optional]
**mobile_phone** | **string** |  | [optional]
**mobile_phone_activation_status** | **int** | enum MobilePhoneActivationStatus | [optional]
**create_date** | **\DateTime** |  | [optional]
**created_by** | **string** |  | [optional]
**spam** | **bool** |  | [optional]
**sid** | **string** | LDAP identifier. REVIEW. | [optional]
**ldap_qouta** | **int** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW. | [optional]
**sso_name_id** | **string** | SAML identifier. REVIEW. | [optional]
**sso_session_id** | **string** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire. | [optional]
**is_active** | **bool** | computed getter | [optional] [readonly]
**check_activation** | **bool** | computed getter | [optional] [readonly]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
