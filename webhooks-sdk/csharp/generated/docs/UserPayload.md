# DocSpace.Webhooks.SDK.Model.UserPayload
ASC.Core.Common/Core/UserInfo.cs - - the domain entity, verbatim. It carries NO [JsonIgnore] at all, so every public member below reaches the wire. sid / ssoNameId / ssoSessionId / ldapQouta are null on portals without LDAP or SSO and therefore invisible in most test captures; they DO appear on LDAP/SSO tenants, and are flagged REVIEW below. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** |  | [optional] 
**FirstName** | **string** |  | [optional] 
**LastName** | **string** |  | [optional] 
**UserName** | **string** |  | [optional] 
**Email** | **string** |  | [optional] 
**BirthDate** | **DateTime** |  | [optional] 
**Sex** | **bool** |  | [optional] 
**Status** | **int** | enum EmployeeStatus | [optional] 
**ActivationStatus** | **int** | enum EmployeeActivationStatus (flags) | [optional] 
**TerminatedDate** | **DateTime** |  | [optional] 
**Title** | **string** |  | [optional] 
**WorkFromDate** | **DateTime** |  | [optional] 
**Location** | **string** |  | [optional] 
**Notes** | **string** |  | [optional] 
**Contacts** | **string** | Flattened form of contactsList. BOTH are emitted - - the same data twice.  | [optional] 
**ContactsList** | **List&lt;string&gt;** |  | [optional] 
**Removed** | **bool** |  | [optional] 
**LastModified** | **DateTime** |  | [optional] 
**TenantId** | **int** |  | [optional] 
**CultureName** | **string** |  | [optional] 
**MobilePhone** | **string** |  | [optional] 
**MobilePhoneActivationStatus** | **int** | enum MobilePhoneActivationStatus | [optional] 
**CreateDate** | **DateTime** |  | [optional] 
**CreatedBy** | **Guid** |  | [optional] 
**Spam** | **bool** |  | [optional] 
**Sid** | **string** | LDAP identifier. REVIEW. | [optional] 
**LdapQouta** | **long** | sic - - misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  | [optional] 
**SsoNameId** | **string** | SAML identifier. REVIEW. | [optional] 
**SsoSessionId** | **string** | SAML SESSION identifier. REVIEW - - should almost certainly not be on the wire.  | [optional] 
**IsActive** | **bool** | computed getter | [optional] [readonly] 
**CheckActivation** | **bool** | computed getter | [optional] [readonly] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

