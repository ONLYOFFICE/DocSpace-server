# FormSubmitPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**OriginalForm** | Pointer to [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] 
**SubmittedForm** | Pointer to [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] 

## Methods

### NewFormSubmitPayload

`func NewFormSubmitPayload() *FormSubmitPayload`

NewFormSubmitPayload instantiates a new FormSubmitPayload object
This constructor will assign default values to properties that have it defined,
and makes sure properties required by API are set, but the set of arguments
will change when the set of required properties is changed

### NewFormSubmitPayloadWithDefaults

`func NewFormSubmitPayloadWithDefaults() *FormSubmitPayload`

NewFormSubmitPayloadWithDefaults instantiates a new FormSubmitPayload object
This constructor will only assign default values to properties that have it defined,
but it doesn't guarantee that properties required by API are set

### GetOriginalForm

`func (o *FormSubmitPayload) GetOriginalForm() FileEntryPayload`

GetOriginalForm returns the OriginalForm field if non-nil, zero value otherwise.

### GetOriginalFormOk

`func (o *FormSubmitPayload) GetOriginalFormOk() (*FileEntryPayload, bool)`

GetOriginalFormOk returns a tuple with the OriginalForm field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetOriginalForm

`func (o *FormSubmitPayload) SetOriginalForm(v FileEntryPayload)`

SetOriginalForm sets OriginalForm field to given value.

### HasOriginalForm

`func (o *FormSubmitPayload) HasOriginalForm() bool`

HasOriginalForm returns a boolean if a field has been set.

### GetSubmittedForm

`func (o *FormSubmitPayload) GetSubmittedForm() FileEntryPayload`

GetSubmittedForm returns the SubmittedForm field if non-nil, zero value otherwise.

### GetSubmittedFormOk

`func (o *FormSubmitPayload) GetSubmittedFormOk() (*FileEntryPayload, bool)`

GetSubmittedFormOk returns a tuple with the SubmittedForm field if it's non-nil, zero value otherwise
and a boolean to check if the value has been set.

### SetSubmittedForm

`func (o *FormSubmitPayload) SetSubmittedForm(v FileEntryPayload)`

SetSubmittedForm sets SubmittedForm field to given value.

### HasSubmittedForm

`func (o *FormSubmitPayload) HasSubmittedForm() bool`

HasSubmittedForm returns a boolean if a field has been set.


[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


