#!/usr/bin/env ruby
# frozen_string_literal: true

# Logs a DocSpace webhook delivery to the console.
#
#     ruby log_webhook.rb                          # replay the shared fixture
#     ruby log_webhook.rb <body> <secret> <sig>
#
# Ruby has no src/ runtime yet, so signature checking lives here.
#
# Regeneration (the generate-webhook-sdk skill) runs this over every fixture
# in ../../samples after each generation.

require 'json'
require 'openssl'
require 'pathname'

ROOT     = Pathname.new(__dir__).parent
FIXTURES = ROOT.parent.join('samples')

# The gem entry point loads ApiModelBase and then every model; the models
# cross-reference each other through const_get, so all of them must be loaded.
$LOAD_PATH.unshift(ROOT.join('generated', 'lib').to_s)
require 'docspace-webhooks-sdk'

SIGNATURE_HEADER = 'x-docspace-signature-256'

# `sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash.
def compute_signature(body, secret)
  "sha256=#{OpenSSL::HMAC.hexdigest('SHA256', secret, body).upcase}"
end

# Constant-time, case-insensitive check over the RAW body. DocSpace emits
# uppercase hex where GitHub emits lowercase, so the comparison folds case.
#
# OpenSSL.secure_compare needs the openssl gem 2.2+ (Ruby 2.7+).
def verify_signature(body, secret, signature)
  return false if signature.nil? || signature.strip.empty?

  OpenSSL.secure_compare(
    compute_signature(body, secret).downcase,
    signature.strip.downcase
  )
end

# The payload model the generated trigger table names: "untyped" for the legacy
# catch-all, "unknown" for a trigger newer than this SDK.
def model_name(trigger)
  DocspaceWebhooksSdk::WebhookTriggers::PAYLOAD_TYPES.fetch(trigger) do
    DocspaceWebhooksSdk::WebhookTriggers::UNTYPED_TRIGGERS.include?(trigger) ? 'untyped' : 'unknown'
  end
end

body_path = ARGV[0] || FIXTURES.join('file.created.json').to_s
secret    = ARGV[1] || File.read(FIXTURES.join('SECRET')).strip
signature = ARGV[2] || File.read(FIXTURES.join('file.created.sig')).strip

# Binary read: the signature covers exactly the bytes that arrived.
body = File.binread(body_path)

unless verify_signature(body, secret, signature)
  warn 'REJECTED: signature does not match'
  exit 1
end

envelope = JSON.parse(body)
trigger  = envelope.dig('event', 'trigger')

if trigger.nil? || trigger.empty?
  warn 'envelope has no event.trigger'
  exit 1
end

event   = DocspaceWebhooksSdk::WebhookEventInfo.build_from_hash(envelope['event'])
config  = DocspaceWebhooksSdk::WebhookConfigInfo.build_from_hash(envelope['webhook'])
# The catch-all, or a trigger added on the server after this SDK was generated,
# is acknowledged with its raw payload and never parsed with some other model.
model = DocspaceWebhooksSdk::WebhookTriggers.payload_type(trigger)
begin
  payload = model&.build_from_hash(envelope['payload'])
rescue StandardError => e
  warn "unparseable #{trigger} payload: #{e.message}"
  exit 1
end
if model && !payload.is_a?(model)
  warn "unparseable #{trigger} payload: not a #{model_name(trigger)}"
  exit 1
end

puts trigger
puts '  signature ok'
puts "  model: #{model_name(trigger)}"
puts format('  event #%s  at %s  by %s', event.id, event.create_on, event.create_by)
puts format('  subscription #%s "%s"', config.id, config.name)

case payload
when DocspaceWebhooksSdk::FilePayload, DocspaceWebhooksSdk::FolderPayload, DocspaceWebhooksSdk::RoomPayload
  # EntryId.build returns the scalar itself: an Integer, or a String for a
  # third-party entry.
  kind = { DocspaceWebhooksSdk::RoomPayload => 'room', DocspaceWebhooksSdk::FolderPayload => 'folder' }
         .fetch(payload.class, 'file')
  puts format('  %s: %s  id=%s  parent=%s',
              kind, payload.title, payload.id, payload.parent_id)
when DocspaceWebhooksSdk::FormSubmitPayload
  puts format('  form: %s  submitted as %s',
              payload.original_form&.title, payload.submitted_form&.title)
when DocspaceWebhooksSdk::UserPayload
  puts format('  user: %s <%s>', payload.user_name, payload.email)
when DocspaceWebhooksSdk::GroupPayload
  puts format('  group: %s', payload.name)
else
  puts '  no payload model for this trigger; raw payload below'
end

puts
puts JSON.pretty_generate(envelope['payload']).gsub(/^/, '  ')
