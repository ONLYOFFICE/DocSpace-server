

# UserPayload

ASC.Core.Common/Core/UserInfo.cs -- the domain entity, verbatim. It carries NO [JsonIgnore] at all, so every public member below reaches the wire. sid / ssoNameId / ssoSessionId / ldapQouta are null on portals without LDAP or SSO and therefore invisible in most test captures; they DO appear on LDAP/SSO tenants, and are flagged REVIEW below. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **UUID** |  |  [optional] |
|**firstName** | **String** |  |  [optional] |
|**lastName** | **String** |  |  [optional] |
|**userName** | **String** |  |  [optional] |
|**email** | **String** |  |  [optional] |
|**birthDate** | **OffsetDateTime** |  |  [optional] |
|**sex** | **Boolean** |  |  [optional] |
|**status** | **Integer** | enum EmployeeStatus |  [optional] |
|**activationStatus** | **Integer** | enum EmployeeActivationStatus (flags) |  [optional] |
|**terminatedDate** | **OffsetDateTime** |  |  [optional] |
|**title** | **String** |  |  [optional] |
|**workFromDate** | **OffsetDateTime** |  |  [optional] |
|**location** | **String** |  |  [optional] |
|**notes** | **String** |  |  [optional] |
|**contacts** | **String** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  |  [optional] |
|**contactsList** | **List&lt;String&gt;** |  |  [optional] |
|**removed** | **Boolean** |  |  [optional] |
|**lastModified** | **OffsetDateTime** |  |  [optional] |
|**tenantId** | **Integer** |  |  [optional] |
|**cultureName** | **String** |  |  [optional] |
|**mobilePhone** | **String** |  |  [optional] |
|**mobilePhoneActivationStatus** | **Integer** | enum MobilePhoneActivationStatus |  [optional] |
|**createDate** | **OffsetDateTime** |  |  [optional] |
|**createdBy** | **UUID** |  |  [optional] |
|**spam** | **Boolean** |  |  [optional] |
|**sid** | **String** | LDAP identifier. REVIEW. |  [optional] |
|**ldapQouta** | **Long** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  |  [optional] |
|**ssoNameId** | **String** | SAML identifier. REVIEW. |  [optional] |
|**ssoSessionId** | **String** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  |  [optional] |
|**isActive** | **Boolean** | computed getter |  [optional] [readonly] |
|**checkActivation** | **Boolean** | computed getter |  [optional] [readonly] |



