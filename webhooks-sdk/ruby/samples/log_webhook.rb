#!/usr/bin/env ruby
# frozen_string_literal: true

# Logs a DocSpace webhook delivery to the console.
#
#     ruby log_webhook.rb                          # replay the shared fixture
#     ruby log_webhook.rb <body> <secret> <sig> [<x-docspace-event-id>]
#
# Regeneration (the generate-webhook-sdk skill) runs this over every fixture
# in ../../samples after each generation.

require 'json'
require 'pathname'

ROOT     = Pathname.new(__dir__).parent
FIXTURES = ROOT.parent.join('samples')

# The gem entry point loads ApiModelBase, every model, the trigger table and
# the router; the models cross-reference each other through const_get, so all
# of them must be loaded.
$LOAD_PATH.unshift(ROOT.join('generated', 'lib').to_s)
require 'docspace-webhooks-sdk'

include DocspaceWebhooksSdk

# The payload model the generated trigger table names: "untyped" for the legacy
# catch-all, "unknown" for a trigger newer than this SDK.
def model_name(trigger)
  WebhookTriggers::PAYLOAD_TYPES.fetch(trigger) do
    WebhookTriggers::UNTYPED_TRIGGERS.include?(trigger) ? 'untyped' : 'unknown'
  end
end

def print_header(trigger, event, webhook, delivery)
  known = model_name(trigger) == 'unknown' ? '  (unknown to this build)' : ''
  puts "#{trigger}#{known}"
  puts '  signature ok'
  puts "  model: #{model_name(trigger)}"
  puts "  delivery: #{delivery}"
  puts format('  event #%s  at %s  by %s', event&.id, event&.create_on, event&.create_by)
  puts format('  subscription #%s "%s"', webhook&.id, webhook&.name)
end

def print_payload(payload)
  case payload
  when FilePayload, FolderPayload, RoomPayload
    # EntryId.build returns the scalar itself: an Integer, or a String for a
    # third-party entry.
    kind = { RoomPayload => 'room', FolderPayload => 'folder' }.fetch(payload.class, 'file')
    puts format('  %s: %s  id=%s  parent=%s', kind, payload.title, payload.id, payload.parent_id)
  when FormSubmitPayload
    puts format('  form: %s  submitted as %s', payload.original_form&.title, payload.submitted_form&.title)
  when UserPayload
    puts format('  user: %s <%s>', payload.user_name, payload.email)
  when GroupPayload
    puts format('  group: %s', payload.name)
  else
    puts '  no payload model for this trigger; raw payload below'
  end
end

def print_raw(payload)
  puts
  puts JSON.pretty_generate(payload).gsub(/^/, '  ')
end

body_path       = ARGV[0] || FIXTURES.join('file.created.json').to_s
secret          = ARGV[1] || File.read(FIXTURES.join('SECRET')).strip
signature       = ARGV[2] || File.read(FIXTURES.join('file.created.sig')).strip
event_id_header = ARGV[3]

# The router checks the signature, drops an event it has processed before
# (DocSpace delivers at least once, and a retry carries the same event.id),
# reads the body into the delivery model of its trigger and calls the handlers.
# The store lives in memory for the sake of the sample; a real receiver persists
# the ids for as long as an event can still arrive -- the automatic retries span
# about 18 hours on the default schedule, and a manual retry can come at any
# time later.
router = WebhookRouter.new(secret, store: InMemoryEventStore.new)

# Binary read: the signature covers exactly the bytes that arrived.
body = File.binread(body_path)

# One handler for every trigger with a typed body, from the generated table. A
# receiver normally registers only the triggers it cares about.
router.on(*WebhookTriggers::DELIVERY_TYPES.keys) do |delivery, trigger|
  print_header(trigger, delivery.event, delivery.webhook,
               WebhookTriggers.delivery_type(trigger).name.split('::').last)
  print_payload(delivery.payload)
  # The payload as it arrived; a handler runs only after the router verified it.
  print_raw(JSON.parse(body)['payload'])
end

# A handler for one trigger: its argument is a FileDelivery.
router.on('file.created') do |delivery, _trigger|
  puts format('  on(file.created): "%s" in folder %s', delivery.payload.title, delivery.payload.parent_id)
end

# Whatever has no handler: the catch-all '*', and triggers newer than this SDK.
router.on_other do |hook|
  print_header(hook.trigger, hook.event, hook.webhook, 'none')
  print_payload(nil)
  print_raw(hook.payload)
end

# In an HTTP handler, answer with result.status_code: a 2xx acknowledges the
# delivery, anything else makes DocSpace send it again.
result = router.handle(body, signature, event_id_header)

case result.outcome
when :rejected  then warn 'REJECTED: signature does not match'
when :invalid   then warn "unparseable body: #{result.error}"
when :failed    then warn "a handler failed: #{result.error}"
when :duplicate then puts "duplicate of event ##{result.event_id}, already processed; skipped"
end

exit(result.success? ? 0 : 1)
