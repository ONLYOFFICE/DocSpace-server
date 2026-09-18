# DocSpace.Webhooks.SDK.Model.GroupPayload
ASC.Core.Common/Core/GroupInfo.cs. Note `ID` in C#; the camelCase policy lowercases the whole leading run, so it is \"id\" on the wire, while `CategoryID` becomes \"categoryID\". 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** |  | [optional] 
**Name** | **string** |  | [optional] 
**CategoryID** | **Guid** |  | [optional] 
**Parent** | [**GroupPayload**](GroupPayload.md) |  | [optional] 
**Sid** | **string** | LDAP identifier. REVIEW. | [optional] 
**Removed** | **bool** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

