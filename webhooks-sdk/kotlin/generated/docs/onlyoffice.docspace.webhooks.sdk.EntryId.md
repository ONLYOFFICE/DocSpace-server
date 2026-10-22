
# EntryId

Entry identifier: a number for entries in DocSpace's own storage, a string for entries backed by a third-party provider. Both occur on the same portal.

Hand-written (`kotlin/src/main/kotlin/onlyoffice/docspace/webhooks/sdk/EntryId.kt`): the generator renders the contract's `oneOf[integer, string]` as an empty class that drops the value, so the generator is told to map the schema onto this type instead (`--schema-mappings`).

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **value** | **kotlin.Any** | The id as it was on the wire: a `Long` or a `String`. |  |
| **asLong** | **kotlin.Long** | The numeric id, or null for a third-party entry. |  [optional] |

Register `EntryIdAdapter()` with Moshi before `KotlinJsonAdapterFactory`, or the models cannot be read.

