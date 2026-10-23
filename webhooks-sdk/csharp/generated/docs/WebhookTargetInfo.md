# DocSpace.Webhooks.SDK.Model.WebhookTargetInfo
The entity the subscription is scoped to. Present only when the subscription is scoped to one entity.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | [**EntryId**](EntryId.md) |  | [optional] 
**Type** | **string** | The prefix of the trigger the delivery is for: user, group, file, folder, room, form or agent today. Deliberately not an enum: every new trigger family adds a value, and a receiver built against an older contract must keep parsing it. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

