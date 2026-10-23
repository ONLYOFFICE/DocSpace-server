# DocspaceWebhooksSdk::WebhookConfigInfo

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **Integer** | The subscription ID. | [optional] |
| **name** | **String** | The subscription name given by its creator. | [optional] |
| **url** | **String** | The URL the delivery is posted to. | [optional] |
| **triggers** | **Array&lt;String&gt;** | The subscribed events, or [\&quot;*\&quot;] for the catch-all. | [optional] |
| **target** | [**WebhookTargetInfo**](WebhookTargetInfo.md) |  | [optional] |
| **last_failure_on** | **Time** | When the last delivery to this subscription failed, in UTC; absent when none has failed. | [optional] |
| **last_failure_content** | **String** | What the receiver answered, or the network error, when the last delivery to this subscription failed. | [optional] |
| **last_success_on** | **Time** | When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet. | [optional] |
| **retry_count** | **Integer** | How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2. | [optional] |
| **retry_on** | **Time** | When the next retry is scheduled, in UTC; absent when no retry is pending. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::WebhookConfigInfo.new(
  id: null,
  name: null,
  url: null,
  triggers: null,
  target: null,
  last_failure_on: null,
  last_failure_content: null,
  last_success_on: null,
  retry_count: null,
  retry_on: null
)
```

