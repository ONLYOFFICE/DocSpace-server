
# UserPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **firstName** | **kotlin.String** |  |  [optional] |
| **lastName** | **kotlin.String** |  |  [optional] |
| **userName** | **kotlin.String** |  |  [optional] |
| **email** | **kotlin.String** |  |  [optional] |
| **birthDate** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **sex** | **kotlin.Boolean** |  |  [optional] |
| **status** | **kotlin.Int** | enum EmployeeStatus |  [optional] |
| **activationStatus** | **kotlin.Int** | enum EmployeeActivationStatus (flags) |  [optional] |
| **terminatedDate** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **title** | **kotlin.String** |  |  [optional] |
| **workFromDate** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **location** | **kotlin.String** |  |  [optional] |
| **notes** | **kotlin.String** |  |  [optional] |
| **contacts** | **kotlin.String** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  |  [optional] |
| **contactsList** | **kotlin.collections.List&lt;kotlin.String&gt;** |  |  [optional] |
| **removed** | **kotlin.Boolean** |  |  [optional] |
| **lastModified** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **tenantId** | **kotlin.Int** |  |  [optional] |
| **cultureName** | **kotlin.String** |  |  [optional] |
| **mobilePhone** | **kotlin.String** |  |  [optional] |
| **mobilePhoneActivationStatus** | **kotlin.Int** | enum MobilePhoneActivationStatus |  [optional] |
| **createDate** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **createdBy** | [**java.util.UUID**](java.util.UUID.md) |  |  [optional] |
| **spam** | **kotlin.Boolean** |  |  [optional] |
| **sid** | **kotlin.String** | LDAP identifier. REVIEW. |  [optional] |
| **ldapQouta** | **kotlin.Long** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  |  [optional] |
| **ssoNameId** | **kotlin.String** | SAML identifier. REVIEW. |  [optional] |
| **ssoSessionId** | **kotlin.String** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  |  [optional] |
| **isActive** | **kotlin.Boolean** | computed getter |  [optional] [readonly] |
| **checkActivation** | **kotlin.Boolean** | computed getter |  [optional] [readonly] |



