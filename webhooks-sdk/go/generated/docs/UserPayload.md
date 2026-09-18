# UserPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **string** |  | [optional] 
**FirstName** | Pointer to **string** |  | [optional] 
**LastName** | Pointer to **string** |  | [optional] 
**UserName** | Pointer to **string** |  | [optional] 
**Email** | Pointer to **string** |  | [optional] 
**BirthDate** | Pointer to **time.Time** |  | [optional] 
**Sex** | Pointer to **bool** |  | [optional] 
**Status** | Pointer to **int32** | enum EmployeeStatus | [optional] 
**ActivationStatus** | Pointer to **int32** | enum EmployeeActivationStatus (flags) | [optional] 
**TerminatedDate** | Pointer to **time.Time** |  | [optional] 
**Title** | Pointer to **string** |  | [optional] 
**WorkFromDate** | Pointer to **time.Time** |  | [optional] 
**Location** | Pointer to **string** |  | [optional] 
**Notes** | Pointer to **string** |  | [optional] 
**Contacts** | Pointer to **string** | Flattened form of contactsList. BOTH are emitted -- the same data twice.  | [optional] 
**ContactsList** | Pointer to **[]string** |  | [optional] 
**Removed** | Pointer to **bool** |  | [optional] 
**LastModified** | Pointer to **time.Time** |  | [optional] 
**TenantId** | Pointer to **int32** |  | [optional] 
**CultureName** | Pointer to **string** |  | [optional] 
**MobilePhone** | Pointer to **string** |  | [optional] 
**MobilePhoneActivationStatus** | Pointer to **int32** | enum MobilePhoneActivationStatus | [optional] 
**CreateDate** | Pointer to **time.Time** |  | [optional] 
**CreatedBy** | Pointer to **string** |  | [optional] 
**Spam** | Pointer to **bool** |  | [optional] 
**Sid** | Pointer to **string** | LDAP identifier. REVIEW. | [optional] 
**LdapQouta** | Pointer to **int64** | sic -- misspelled in the domain type, and misspelled on the wire. LDAP quota. REVIEW.  | [optional] 
**SsoNameId** | Pointer to **string** | SAML identifier. REVIEW. | [optional] 
**SsoSessionId** | Pointer to **string** | SAML SESSION identifier. REVIEW -- should almost certainly not be on the wire.  | [optional] 
**IsActive** | Pointer to **bool** | computed getter | [optional] [readonly] 
**CheckActivation** | Pointer to **bool** | computed getter | [optional] [readonly] 

## Methods

### NewUserPayload

`func NewUserPayload() *UserPayload`

NewUserPayload instantiates a new UserPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewUserPayloadWithDefaults

`func NewUserPayloadWithDefaults() *UserPayload`

NewUserPayloadWithDefaults instantiates a new UserPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *UserPayload) GetId() string`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *UserPayload) GetIdOk() (*string, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *UserPayload) SetId(v string)`

SetId sets Id field to given value.

### HasId

`func (o *UserPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetFirstName

`func (o *UserPayload) GetFirstName() string`

GetFirstName returns the FirstName field if non-nil, zero value otherwise.

### GetFirstNameOk

`func (o *UserPayload) GetFirstNameOk() (*string, bool)`

GetFirstNameOk returns a tuple with the FirstName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetFirstName

`func (o *UserPayload) SetFirstName(v string)`

SetFirstName sets FirstName field to given value.

### HasFirstName

`func (o *UserPayload) HasFirstName() bool`

HasFirstName returns a boolean if a field has been set.

### GetLastName

`func (o *UserPayload) GetLastName() string`

GetLastName returns the LastName field if non-nil, zero value otherwise.

### GetLastNameOk

`func (o *UserPayload) GetLastNameOk() (*string, bool)`

GetLastNameOk returns a tuple with the LastName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastName

`func (o *UserPayload) SetLastName(v string)`

SetLastName sets LastName field to given value.

### HasLastName

`func (o *UserPayload) HasLastName() bool`

HasLastName returns a boolean if a field has been set.

### GetUserName

`func (o *UserPayload) GetUserName() string`

GetUserName returns the UserName field if non-nil, zero value otherwise.

### GetUserNameOk

`func (o *UserPayload) GetUserNameOk() (*string, bool)`

GetUserNameOk returns a tuple with the UserName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUserName

`func (o *UserPayload) SetUserName(v string)`

SetUserName sets UserName field to given value.

### HasUserName

`func (o *UserPayload) HasUserName() bool`

HasUserName returns a boolean if a field has been set.

### GetEmail

`func (o *UserPayload) GetEmail() string`

GetEmail returns the Email field if non-nil, zero value otherwise.

### GetEmailOk

`func (o *UserPayload) GetEmailOk() (*string, bool)`

GetEmailOk returns a tuple with the Email field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetEmail

`func (o *UserPayload) SetEmail(v string)`

SetEmail sets Email field to given value.

### HasEmail

`func (o *UserPayload) HasEmail() bool`

HasEmail returns a boolean if a field has been set.

### GetBirthDate

`func (o *UserPayload) GetBirthDate() time.Time`

GetBirthDate returns the BirthDate field if non-nil, zero value otherwise.

### GetBirthDateOk

`func (o *UserPayload) GetBirthDateOk() (*time.Time, bool)`

GetBirthDateOk returns a tuple with the BirthDate field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetBirthDate

`func (o *UserPayload) SetBirthDate(v time.Time)`

SetBirthDate sets BirthDate field to given value.

### HasBirthDate

`func (o *UserPayload) HasBirthDate() bool`

HasBirthDate returns a boolean if a field has been set.

### GetSex

`func (o *UserPayload) GetSex() bool`

GetSex returns the Sex field if non-nil, zero value otherwise.

### GetSexOk

`func (o *UserPayload) GetSexOk() (*bool, bool)`

GetSexOk returns a tuple with the Sex field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSex

`func (o *UserPayload) SetSex(v bool)`

SetSex sets Sex field to given value.

### HasSex

`func (o *UserPayload) HasSex() bool`

HasSex returns a boolean if a field has been set.

### GetStatus

`func (o *UserPayload) GetStatus() int32`

GetStatus returns the Status field if non-nil, zero value otherwise.

### GetStatusOk

`func (o *UserPayload) GetStatusOk() (*int32, bool)`

GetStatusOk returns a tuple with the Status field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetStatus

`func (o *UserPayload) SetStatus(v int32)`

SetStatus sets Status field to given value.

### HasStatus

`func (o *UserPayload) HasStatus() bool`

HasStatus returns a boolean if a field has been set.

### GetActivationStatus

`func (o *UserPayload) GetActivationStatus() int32`

GetActivationStatus returns the ActivationStatus field if non-nil, zero value otherwise.

### GetActivationStatusOk

`func (o *UserPayload) GetActivationStatusOk() (*int32, bool)`

GetActivationStatusOk returns a tuple with the ActivationStatus field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetActivationStatus

`func (o *UserPayload) SetActivationStatus(v int32)`

SetActivationStatus sets ActivationStatus field to given value.

### HasActivationStatus

`func (o *UserPayload) HasActivationStatus() bool`

HasActivationStatus returns a boolean if a field has been set.

### GetTerminatedDate

`func (o *UserPayload) GetTerminatedDate() time.Time`

GetTerminatedDate returns the TerminatedDate field if non-nil, zero value otherwise.

### GetTerminatedDateOk

`func (o *UserPayload) GetTerminatedDateOk() (*time.Time, bool)`

GetTerminatedDateOk returns a tuple with the TerminatedDate field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTerminatedDate

`func (o *UserPayload) SetTerminatedDate(v time.Time)`

SetTerminatedDate sets TerminatedDate field to given value.

### HasTerminatedDate

`func (o *UserPayload) HasTerminatedDate() bool`

HasTerminatedDate returns a boolean if a field has been set.

### GetTitle

`func (o *UserPayload) GetTitle() string`

GetTitle returns the Title field if non-nil, zero value otherwise.

### GetTitleOk

`func (o *UserPayload) GetTitleOk() (*string, bool)`

GetTitleOk returns a tuple with the Title field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTitle

`func (o *UserPayload) SetTitle(v string)`

SetTitle sets Title field to given value.

### HasTitle

`func (o *UserPayload) HasTitle() bool`

HasTitle returns a boolean if a field has been set.

### GetWorkFromDate

`func (o *UserPayload) GetWorkFromDate() time.Time`

GetWorkFromDate returns the WorkFromDate field if non-nil, zero value otherwise.

### GetWorkFromDateOk

`func (o *UserPayload) GetWorkFromDateOk() (*time.Time, bool)`

GetWorkFromDateOk returns a tuple with the WorkFromDate field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetWorkFromDate

`func (o *UserPayload) SetWorkFromDate(v time.Time)`

SetWorkFromDate sets WorkFromDate field to given value.

### HasWorkFromDate

`func (o *UserPayload) HasWorkFromDate() bool`

HasWorkFromDate returns a boolean if a field has been set.

### GetLocation

`func (o *UserPayload) GetLocation() string`

GetLocation returns the Location field if non-nil, zero value otherwise.

### GetLocationOk

`func (o *UserPayload) GetLocationOk() (*string, bool)`

GetLocationOk returns a tuple with the Location field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLocation

`func (o *UserPayload) SetLocation(v string)`

SetLocation sets Location field to given value.

### HasLocation

`func (o *UserPayload) HasLocation() bool`

HasLocation returns a boolean if a field has been set.

### GetNotes

`func (o *UserPayload) GetNotes() string`

GetNotes returns the Notes field if non-nil, zero value otherwise.

### GetNotesOk

`func (o *UserPayload) GetNotesOk() (*string, bool)`

GetNotesOk returns a tuple with the Notes field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetNotes

`func (o *UserPayload) SetNotes(v string)`

SetNotes sets Notes field to given value.

### HasNotes

`func (o *UserPayload) HasNotes() bool`

HasNotes returns a boolean if a field has been set.

### GetContacts

`func (o *UserPayload) GetContacts() string`

GetContacts returns the Contacts field if non-nil, zero value otherwise.

### GetContactsOk

`func (o *UserPayload) GetContactsOk() (*string, bool)`

GetContactsOk returns a tuple with the Contacts field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetContacts

`func (o *UserPayload) SetContacts(v string)`

SetContacts sets Contacts field to given value.

### HasContacts

`func (o *UserPayload) HasContacts() bool`

HasContacts returns a boolean if a field has been set.

### GetContactsList

`func (o *UserPayload) GetContactsList() []string`

GetContactsList returns the ContactsList field if non-nil, zero value otherwise.

### GetContactsListOk

`func (o *UserPayload) GetContactsListOk() (*[]string, bool)`

GetContactsListOk returns a tuple with the ContactsList field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetContactsList

`func (o *UserPayload) SetContactsList(v []string)`

SetContactsList sets ContactsList field to given value.

### HasContactsList

`func (o *UserPayload) HasContactsList() bool`

HasContactsList returns a boolean if a field has been set.

### GetRemoved

`func (o *UserPayload) GetRemoved() bool`

GetRemoved returns the Removed field if non-nil, zero value otherwise.

### GetRemovedOk

`func (o *UserPayload) GetRemovedOk() (*bool, bool)`

GetRemovedOk returns a tuple with the Removed field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRemoved

`func (o *UserPayload) SetRemoved(v bool)`

SetRemoved sets Removed field to given value.

### HasRemoved

`func (o *UserPayload) HasRemoved() bool`

HasRemoved returns a boolean if a field has been set.

### GetLastModified

`func (o *UserPayload) GetLastModified() time.Time`

GetLastModified returns the LastModified field if non-nil, zero value otherwise.

### GetLastModifiedOk

`func (o *UserPayload) GetLastModifiedOk() (*time.Time, bool)`

GetLastModifiedOk returns a tuple with the LastModified field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastModified

`func (o *UserPayload) SetLastModified(v time.Time)`

SetLastModified sets LastModified field to given value.

### HasLastModified

`func (o *UserPayload) HasLastModified() bool`

HasLastModified returns a boolean if a field has been set.

### GetTenantId

`func (o *UserPayload) GetTenantId() int32`

GetTenantId returns the TenantId field if non-nil, zero value otherwise.

### GetTenantIdOk

`func (o *UserPayload) GetTenantIdOk() (*int32, bool)`

GetTenantIdOk returns a tuple with the TenantId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTenantId

`func (o *UserPayload) SetTenantId(v int32)`

SetTenantId sets TenantId field to given value.

### HasTenantId

`func (o *UserPayload) HasTenantId() bool`

HasTenantId returns a boolean if a field has been set.

### GetCultureName

`func (o *UserPayload) GetCultureName() string`

GetCultureName returns the CultureName field if non-nil, zero value otherwise.

### GetCultureNameOk

`func (o *UserPayload) GetCultureNameOk() (*string, bool)`

GetCultureNameOk returns a tuple with the CultureName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCultureName

`func (o *UserPayload) SetCultureName(v string)`

SetCultureName sets CultureName field to given value.

### HasCultureName

`func (o *UserPayload) HasCultureName() bool`

HasCultureName returns a boolean if a field has been set.

### GetMobilePhone

`func (o *UserPayload) GetMobilePhone() string`

GetMobilePhone returns the MobilePhone field if non-nil, zero value otherwise.

### GetMobilePhoneOk

`func (o *UserPayload) GetMobilePhoneOk() (*string, bool)`

GetMobilePhoneOk returns a tuple with the MobilePhone field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetMobilePhone

`func (o *UserPayload) SetMobilePhone(v string)`

SetMobilePhone sets MobilePhone field to given value.

### HasMobilePhone

`func (o *UserPayload) HasMobilePhone() bool`

HasMobilePhone returns a boolean if a field has been set.

### GetMobilePhoneActivationStatus

`func (o *UserPayload) GetMobilePhoneActivationStatus() int32`

GetMobilePhoneActivationStatus returns the MobilePhoneActivationStatus field if non-nil, zero value otherwise.

### GetMobilePhoneActivationStatusOk

`func (o *UserPayload) GetMobilePhoneActivationStatusOk() (*int32, bool)`

GetMobilePhoneActivationStatusOk returns a tuple with the MobilePhoneActivationStatus field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetMobilePhoneActivationStatus

`func (o *UserPayload) SetMobilePhoneActivationStatus(v int32)`

SetMobilePhoneActivationStatus sets MobilePhoneActivationStatus field to given value.

### HasMobilePhoneActivationStatus

`func (o *UserPayload) HasMobilePhoneActivationStatus() bool`

HasMobilePhoneActivationStatus returns a boolean if a field has been set.

### GetCreateDate

`func (o *UserPayload) GetCreateDate() time.Time`

GetCreateDate returns the CreateDate field if non-nil, zero value otherwise.

### GetCreateDateOk

`func (o *UserPayload) GetCreateDateOk() (*time.Time, bool)`

GetCreateDateOk returns a tuple with the CreateDate field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreateDate

`func (o *UserPayload) SetCreateDate(v time.Time)`

SetCreateDate sets CreateDate field to given value.

### HasCreateDate

`func (o *UserPayload) HasCreateDate() bool`

HasCreateDate returns a boolean if a field has been set.

### GetCreatedBy

`func (o *UserPayload) GetCreatedBy() string`

GetCreatedBy returns the CreatedBy field if non-nil, zero value otherwise.

### GetCreatedByOk

`func (o *UserPayload) GetCreatedByOk() (*string, bool)`

GetCreatedByOk returns a tuple with the CreatedBy field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCreatedBy

`func (o *UserPayload) SetCreatedBy(v string)`

SetCreatedBy sets CreatedBy field to given value.

### HasCreatedBy

`func (o *UserPayload) HasCreatedBy() bool`

HasCreatedBy returns a boolean if a field has been set.

### GetSpam

`func (o *UserPayload) GetSpam() bool`

GetSpam returns the Spam field if non-nil, zero value otherwise.

### GetSpamOk

`func (o *UserPayload) GetSpamOk() (*bool, bool)`

GetSpamOk returns a tuple with the Spam field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSpam

`func (o *UserPayload) SetSpam(v bool)`

SetSpam sets Spam field to given value.

### HasSpam

`func (o *UserPayload) HasSpam() bool`

HasSpam returns a boolean if a field has been set.

### GetSid

`func (o *UserPayload) GetSid() string`

GetSid returns the Sid field if non-nil, zero value otherwise.

### GetSidOk

`func (o *UserPayload) GetSidOk() (*string, bool)`

GetSidOk returns a tuple with the Sid field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSid

`func (o *UserPayload) SetSid(v string)`

SetSid sets Sid field to given value.

### HasSid

`func (o *UserPayload) HasSid() bool`

HasSid returns a boolean if a field has been set.

### GetLdapQouta

`func (o *UserPayload) GetLdapQouta() int64`

GetLdapQouta returns the LdapQouta field if non-nil, zero value otherwise.

### GetLdapQoutaOk

`func (o *UserPayload) GetLdapQoutaOk() (*int64, bool)`

GetLdapQoutaOk returns a tuple with the LdapQouta field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLdapQouta

`func (o *UserPayload) SetLdapQouta(v int64)`

SetLdapQouta sets LdapQouta field to given value.

### HasLdapQouta

`func (o *UserPayload) HasLdapQouta() bool`

HasLdapQouta returns a boolean if a field has been set.

### GetSsoNameId

`func (o *UserPayload) GetSsoNameId() string`

GetSsoNameId returns the SsoNameId field if non-nil, zero value otherwise.

### GetSsoNameIdOk

`func (o *UserPayload) GetSsoNameIdOk() (*string, bool)`

GetSsoNameIdOk returns a tuple with the SsoNameId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSsoNameId

`func (o *UserPayload) SetSsoNameId(v string)`

SetSsoNameId sets SsoNameId field to given value.

### HasSsoNameId

`func (o *UserPayload) HasSsoNameId() bool`

HasSsoNameId returns a boolean if a field has been set.

### GetSsoSessionId

`func (o *UserPayload) GetSsoSessionId() string`

GetSsoSessionId returns the SsoSessionId field if non-nil, zero value otherwise.

### GetSsoSessionIdOk

`func (o *UserPayload) GetSsoSessionIdOk() (*string, bool)`

GetSsoSessionIdOk returns a tuple with the SsoSessionId field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSsoSessionId

`func (o *UserPayload) SetSsoSessionId(v string)`

SetSsoSessionId sets SsoSessionId field to given value.

### HasSsoSessionId

`func (o *UserPayload) HasSsoSessionId() bool`

HasSsoSessionId returns a boolean if a field has been set.

### GetIsActive

`func (o *UserPayload) GetIsActive() bool`

GetIsActive returns the IsActive field if non-nil, zero value otherwise.

### GetIsActiveOk

`func (o *UserPayload) GetIsActiveOk() (*bool, bool)`

GetIsActiveOk returns a tuple with the IsActive field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetIsActive

`func (o *UserPayload) SetIsActive(v bool)`

SetIsActive sets IsActive field to given value.

### HasIsActive

`func (o *UserPayload) HasIsActive() bool`

HasIsActive returns a boolean if a field has been set.

### GetCheckActivation

`func (o *UserPayload) GetCheckActivation() bool`

GetCheckActivation returns the CheckActivation field if non-nil, zero value otherwise.

### GetCheckActivationOk

`func (o *UserPayload) GetCheckActivationOk() (*bool, bool)`

GetCheckActivationOk returns a tuple with the CheckActivation field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetCheckActivation

`func (o *UserPayload) SetCheckActivation(v bool)`

SetCheckActivation sets CheckActivation field to given value.

### HasCheckActivation

`func (o *UserPayload) HasCheckActivation() bool`

HasCheckActivation returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


