# EntryId

Entry identifier. Generic T in the domain types: integer for entries in DocSpace's own storage, string for entries backed by a third-party provider. Both occur on the same portal. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------

## Example

```python
from docspace_webhooks_sdk.models.entry_id import EntryId

# TODO update the JSON string below
json = "{}"
# create an instance of EntryId from a JSON string
entry_id_instance = EntryId.from_json(json)
# print the JSON string representation of the object
print(EntryId.to_json())

# convert the object into a dict
entry_id_dict = entry_id_instance.to_dict()
# create an instance of EntryId from a dict
entry_id_from_dict = EntryId.from_dict(entry_id_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


