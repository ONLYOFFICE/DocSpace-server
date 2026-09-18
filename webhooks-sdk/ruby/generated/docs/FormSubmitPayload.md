# DocspaceWebhooksSdk::FormSubmitPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **original_form** | [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] |
| **submitted_form** | [**FileEntryPayload**](FileEntryPayload.md) |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FormSubmitPayload.new(
  original_form: null,
  submitted_form: null
)
```

