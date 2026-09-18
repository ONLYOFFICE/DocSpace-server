# WebhookConfigInfo

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | Pointer to **int32** |  | [optional] 
**Name** | Pointer to **string** |  | [optional] 
**Url** | Pointer to **string** |  | [optional] 
**Triggers** | Pointer to **[]string** | Subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] 
**Target** | Pointer to [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] 
**LastFailureOn** | Pointer to **time.Time** |  | [optional] 
**LastFailureContent** | Pointer to **string** |  | [optional] 
**LastSuccessOn** | Pointer to **time.Time** |  | [optional] 
**RetryCount** | Pointer to **int32** | Omitted on the first attempt (0). Present from attempt 2. | [optional] 
**RetryOn** | Pointer to **time.Time** |  | [optional] 

## Methods

### NewWebhookConfigInfo

`func NewWebhookConfigInfo() *WebhookConfigInfo`

NewWebhookConfigInfo instantiates a new WebhookConfigInfo object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewWebhookConfigInfoWithDefaults

`func NewWebhookConfigInfoWithDefaults() *WebhookConfigInfo`

NewWebhookConfigInfoWithDefaults instantiates a new WebhookConfigInfo object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetId

`func (o *WebhookConfigInfo) GetId() int32`

GetId returns the Id field if non-nil, zero value otherwise.

### GetIdOk

`func (o *WebhookConfigInfo) GetIdOk() (*int32, bool)`

GetIdOk returns a tuple with the Id field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetId

`func (o *WebhookConfigInfo) SetId(v int32)`

SetId sets Id field to given value.

### HasId

`func (o *WebhookConfigInfo) HasId() bool`

HasId returns a boolean if a field has been set.

### GetName

`func (o *WebhookConfigInfo) GetName() string`

GetName returns the Name field if non-nil, zero value otherwise.

### GetNameOk

`func (o *WebhookConfigInfo) GetNameOk() (*string, bool)`

GetNameOk returns a tuple with the Name field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetName

`func (o *WebhookConfigInfo) SetName(v string)`

SetName sets Name field to given value.

### HasName

`func (o *WebhookConfigInfo) HasName() bool`

HasName returns a boolean if a field has been set.

### GetUrl

`func (o *WebhookConfigInfo) GetUrl() string`

GetUrl returns the Url field if non-nil, zero value otherwise.

### GetUrlOk

`func (o *WebhookConfigInfo) GetUrlOk() (*string, bool)`

GetUrlOk returns a tuple with the Url field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetUrl

`func (o *WebhookConfigInfo) SetUrl(v string)`

SetUrl sets Url field to given value.

### HasUrl

`func (o *WebhookConfigInfo) HasUrl() bool`

HasUrl returns a boolean if a field has been set.

### GetTriggers

`func (o *WebhookConfigInfo) GetTriggers() []string`

GetTriggers returns the Triggers field if non-nil, zero value otherwise.

### GetTriggersOk

`func (o *WebhookConfigInfo) GetTriggersOk() (*[]string, bool)`

GetTriggersOk returns a tuple with the Triggers field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTriggers

`func (o *WebhookConfigInfo) SetTriggers(v []string)`

SetTriggers sets Triggers field to given value.

### HasTriggers

`func (o *WebhookConfigInfo) HasTriggers() bool`

HasTriggers returns a boolean if a field has been set.

### GetTarget

`func (o *WebhookConfigInfo) GetTarget() WebhookTargetInfo`

GetTarget returns the Target field if non-nil, zero value otherwise.

### GetTargetOk

`func (o *WebhookConfigInfo) GetTargetOk() (*WebhookTargetInfo, bool)`

GetTargetOk returns a tuple with the Target field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetTarget

`func (o *WebhookConfigInfo) SetTarget(v WebhookTargetInfo)`

SetTarget sets Target field to given value.

### HasTarget

`func (o *WebhookConfigInfo) HasTarget() bool`

HasTarget returns a boolean if a field has been set.

### GetLastFailureOn

`func (o *WebhookConfigInfo) GetLastFailureOn() time.Time`

GetLastFailureOn returns the LastFailureOn field if non-nil, zero value otherwise.

### GetLastFailureOnOk

`func (o *WebhookConfigInfo) GetLastFailureOnOk() (*time.Time, bool)`

GetLastFailureOnOk returns a tuple with the LastFailureOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastFailureOn

`func (o *WebhookConfigInfo) SetLastFailureOn(v time.Time)`

SetLastFailureOn sets LastFailureOn field to given value.

### HasLastFailureOn

`func (o *WebhookConfigInfo) HasLastFailureOn() bool`

HasLastFailureOn returns a boolean if a field has been set.

### GetLastFailureContent

`func (o *WebhookConfigInfo) GetLastFailureContent() string`

GetLastFailureContent returns the LastFailureContent field if non-nil, zero value otherwise.

### GetLastFailureContentOk

`func (o *WebhookConfigInfo) GetLastFailureContentOk() (*string, bool)`

GetLastFailureContentOk returns a tuple with the LastFailureContent field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastFailureContent

`func (o *WebhookConfigInfo) SetLastFailureContent(v string)`

SetLastFailureContent sets LastFailureContent field to given value.

### HasLastFailureContent

`func (o *WebhookConfigInfo) HasLastFailureContent() bool`

HasLastFailureContent returns a boolean if a field has been set.

### GetLastSuccessOn

`func (o *WebhookConfigInfo) GetLastSuccessOn() time.Time`

GetLastSuccessOn returns the LastSuccessOn field if non-nil, zero value otherwise.

### GetLastSuccessOnOk

`func (o *WebhookConfigInfo) GetLastSuccessOnOk() (*time.Time, bool)`

GetLastSuccessOnOk returns a tuple with the LastSuccessOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetLastSuccessOn

`func (o *WebhookConfigInfo) SetLastSuccessOn(v time.Time)`

SetLastSuccessOn sets LastSuccessOn field to given value.

### HasLastSuccessOn

`func (o *WebhookConfigInfo) HasLastSuccessOn() bool`

HasLastSuccessOn returns a boolean if a field has been set.

### GetRetryCount

`func (o *WebhookConfigInfo) GetRetryCount() int32`

GetRetryCount returns the RetryCount field if non-nil, zero value otherwise.

### GetRetryCountOk

`func (o *WebhookConfigInfo) GetRetryCountOk() (*int32, bool)`

GetRetryCountOk returns a tuple with the RetryCount field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRetryCount

`func (o *WebhookConfigInfo) SetRetryCount(v int32)`

SetRetryCount sets RetryCount field to given value.

### HasRetryCount

`func (o *WebhookConfigInfo) HasRetryCount() bool`

HasRetryCount returns a boolean if a field has been set.

### GetRetryOn

`func (o *WebhookConfigInfo) GetRetryOn() time.Time`

GetRetryOn returns the RetryOn field if non-nil, zero value otherwise.

### GetRetryOnOk

`func (o *WebhookConfigInfo) GetRetryOnOk() (*time.Time, bool)`

GetRetryOnOk returns a tuple with the RetryOn field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetRetryOn

`func (o *WebhookConfigInfo) SetRetryOn(v time.Time)`

SetRetryOn sets RetryOn field to given value.

### HasRetryOn

`func (o *WebhookConfigInfo) HasRetryOn() bool`

HasRetryOn returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


