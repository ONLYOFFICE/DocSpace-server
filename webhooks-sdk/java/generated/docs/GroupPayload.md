

# GroupPayload

ASC.Core.Common/Core/GroupInfo.cs. Note `ID` in C#; the camelCase policy lowercases the whole leading run, so it is \"id\" on the wire, while `CategoryID` becomes \"categoryID\". 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | **UUID** |  |  [optional] |
|**name** | **String** |  |  [optional] |
|**categoryID** | **UUID** |  |  [optional] |
|**parent** | [**GroupPayload**](GroupPayload.md) |  |  [optional] |
|**sid** | **String** | LDAP identifier. REVIEW. |  [optional] |
|**removed** | **Boolean** |  |  [optional] |



