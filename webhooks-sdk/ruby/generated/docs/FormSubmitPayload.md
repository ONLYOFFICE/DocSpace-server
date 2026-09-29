# DocspaceWebhooksSdk::FormSubmitPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **original_form** | [**FilePayload**](FilePayload.md) |  | [optional] |
| **submitted_form** | [**FilePayload**](FilePayload.md) |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FormSubmitPayload.new(
  original_form: null,
  submitted_form: null
)
```

