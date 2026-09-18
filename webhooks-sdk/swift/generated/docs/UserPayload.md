# UserPayload

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **UUID** |  | [optional] 
**firstName** | **String** |  | [optional] 
**lastName** | **String** |  | [optional] 
**userName** | **String** |  | [optional] 
**email** | **String** |  | [optional] 
**birthDate** | **Date** |  | [optional] 
**sex** | **Bool** |  | [optional] 
**status** | **Int** | enum EmployeeStatus | [optional] 
**activationStatus** | **Int** | enum EmployeeActivationStatus (flags) | [optional] 
**terminatedDate** | **Date** |  | [optional] 
**title** | **String** |  | [optional] 
**workFromDate** | **Date** |  | [optional] 
**location** | **String** |  | [optional] 
**notes** | **String** |  | [optional] 
**contacts** | **String** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  | [optional] 
**contactsList** | **[String]** |  | [optional] 
**removed** | **Bool** |  | [optional] 
**lastModified** | **Date** |  | [optional] 
**tenantId** | **Int** |  | [optional] 
**cultureName** | **String** |  | [optional] 
**mobilePhone** | **String** |  | [optional] 
**mobilePhoneActivationStatus** | **Int** | enum MobilePhoneActivationStatus | [optional] 
**createDate** | **Date** |  | [optional] 
**createdBy** | **UUID** |  | [optional] 
**spam** | **Bool** |  | [optional] 
**sid** | **String** | LDAP identifier. REVIEW. | [optional] 
**ldapQouta** | **Int64** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  | [optional] 
**ssoNameId** | **String** | SAML identifier. REVIEW. | [optional] 
**ssoSessionId** | **String** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  | [optional] 
**isActive** | **Bool** | computed getter | [optional] [readonly] 
**checkActivation** | **Bool** | computed getter | [optional] [readonly] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


