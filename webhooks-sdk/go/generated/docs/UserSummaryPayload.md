# UserSummaryPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **string** | The user ID. | [optional] 
**DisplayName** | Pointer to **string** | The HTML-encoded user display name. | [optional] 
**UserName** | Pointer to **string** | The user name. | [optional] 
**Avatar** | Pointer to **string** | The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does. | [optional] 
**ProfileUrl** | Pointer to **string** | The absolute link to the user in the portal&#39;s list of people, or of guests for a guest; today it is a search of that list by the user&#39;s email address. Absent when the subscription&#39;s creator is a User or a Guest, who may not see the contacts of other users, unless this user is that creator. | [optional] 

## Methods

### NewUserSummaryPayload

`func NewUserSummaryPayload() *UserSummaryPayload`

NewUserSummaryPayload instantiates a new UserSummaryPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewUserSummaryPayloadWithDefaults

`func NewUserSummaryPayloadWithDefaults() *UserSummaryPayload`

NewUserSummaryPayloadWithDefaults instantiates a new UserSummaryPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *UserSummaryPayload) GetId() string`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *UserSummaryPayload) GetIdOk() (*string, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *UserSummaryPayload) SetId(v string)`

SetId sets Id field to given value.

### HasId

`func (o *UserSummaryPayload) HasId() bool`

HasId returns a boolean if a field has been set.

### GetDisplayName

`func (o *UserSummaryPayload) GetDisplayName() string`

GetDisplayName returns the DisplayName field if non-nil, zero value otherwise.

### GetDisplayNameOk

`func (o *UserSummaryPayload) GetDisplayNameOk() (*string, bool)`

GetDisplayNameOk returns a tuple with the DisplayName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetDisplayName

`func (o *UserSummaryPayload) SetDisplayName(v string)`

SetDisplayName sets DisplayName field to given value.

### HasDisplayName

`func (o *UserSummaryPayload) HasDisplayName() bool`

HasDisplayName returns a boolean if a field has been set.

### GetUserName

`func (o *UserSummaryPayload) GetUserName() string`

GetUserName returns the UserName field if non-nil, zero value otherwise.

### GetUserNameOk

`func (o *UserSummaryPayload) GetUserNameOk() (*string, bool)`

GetUserNameOk returns a tuple with the UserName field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUserName

`func (o *UserSummaryPayload) SetUserName(v string)`

SetUserName sets UserName field to given value.

### HasUserName

`func (o *UserSummaryPayload) HasUserName() bool`

HasUserName returns a boolean if a field has been set.

### GetAvatar

`func (o *UserSummaryPayload) GetAvatar() string`

GetAvatar returns the Avatar field if non-nil, zero value otherwise.

### GetAvatarOk

`func (o *UserSummaryPayload) GetAvatarOk() (*string, bool)`

GetAvatarOk returns a tuple with the Avatar field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetAvatar

`func (o *UserSummaryPayload) SetAvatar(v string)`

SetAvatar sets Avatar field to given value.

### HasAvatar

`func (o *UserSummaryPayload) HasAvatar() bool`

HasAvatar returns a boolean if a field has been set.

### GetProfileUrl

`func (o *UserSummaryPayload) GetProfileUrl() string`

GetProfileUrl returns the ProfileUrl field if non-nil, zero value otherwise.

### GetProfileUrlOk

`func (o *UserSummaryPayload) GetProfileUrlOk() (*string, bool)`

GetProfileUrlOk returns a tuple with the ProfileUrl field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetProfileUrl

`func (o *UserSummaryPayload) SetProfileUrl(v string)`

SetProfileUrl sets ProfileUrl field to given value.

### HasProfileUrl

`func (o *UserSummaryPayload) HasProfileUrl() bool`

HasProfileUrl returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


