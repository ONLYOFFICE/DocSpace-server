# frozen_string_literal: true

# Webhook router: verifies a delivery, drops a duplicate, reads the body into
# the delivery model of its trigger and calls the handlers registered for it.
#
# Hand-written; lives in ruby/shim/ and is copied into the generated gem by the
# post-step of the generate-webhook-sdk skill. Edit it there, never in
# ruby/generated/.

require 'json'
require 'openssl'

module DocspaceWebhooksSdk
  # What WebhookRouter#handle did with a delivery, and the HTTP status to answer
  # DocSpace with. A 2xx acknowledges the delivery; anything else is a failed
  # attempt, which DocSpace retries on its schedule.
  #
  # outcome is one of WebhookRouter::OUTCOMES:
  #   :handled   - at least one handler ran, and all of them completed
  #   :duplicate - the event was processed before; nothing ran
  #   :unhandled - no handler is registered for the trigger; acknowledged all the same
  #   :rejected  - the signature is missing or does not match
  #   :invalid   - the body is not a delivery, or a typed trigger arrived without its payload
  #   :failed    - a handler raised; the remaining ones did not run
  WebhookHandleResult = Struct.new(:outcome, :status_code, :trigger, :event_id, :error, :cause,
                                   keyword_init: true) do
    def success?
      status_code >= 200 && status_code < 300
    end
  end

  # A delivery no typed handler takes: the catch-all '*', a trigger added after
  # this SDK was built, or a typed trigger with no handler registered. payload
  # is the raw Hash as received; raw is the whole parsed envelope.
  OtherWebhook = Struct.new(:trigger, :event, :webhook, :payload, :raw, keyword_init: true)

  # Remembers which events were processed, in process memory: for samples and
  # tests, lost on restart.
  #
  # Any object with processed?(event_id) and mark_processed(event_id) serves as a
  # store. DocSpace delivers at least once: an automatic or manual retry sends the
  # same event.id again, and the signature does not prove when a request was
  # sent, so the id is the duplicate and replay defence. A real receiver persists
  # the ids for as long as an event can still arrive -- the automatic retries
  # span about 18 hours on the default schedule, and a manual retry can come at
  # any time later.
  class InMemoryEventStore
    def initialize
      @ids = {}
      @lock = Mutex.new
    end

    def processed?(event_id)
      @lock.synchronize { @ids.key?(event_id) }
    end

    # Called only after every handler completed.
    def mark_processed(event_id)
      @lock.synchronize { @ids[event_id] = true }
    end
  end

  # Dispatches verified deliveries to handlers registered per trigger. A handler
  # of a trigger gets the delivery model of that trigger (FileDelivery for
  # 'file.created', ...) and the trigger name. Any number of handlers can be
  # registered for one trigger; on returns a lambda that removes the
  # registration again, and off does the same.
  #
  # handle checks the signature, drops a duplicate, reads the body into the
  # delivery model of its trigger and calls every handler of that trigger in
  # registration order. Answer DocSpace with the result's status_code only after
  # it returns: a handler that raises makes it a failed attempt, and DocSpace
  # sends the delivery again, so handlers have to be idempotent.
  class WebhookRouter
    OUTCOMES = %i[handled duplicate unhandled rejected invalid failed].freeze

    # secret: the secret key of the subscription. store: where processed event
    # ids are kept; without one, duplicates are not detected.
    def initialize(secret, store: nil)
      raise ArgumentError, 'a secret is required; use WebhookRouter.for_unsigned_replay to skip the check' \
        if secret.nil? || secret.to_s.empty?

      setup(secret.to_s, store)
    end

    # A router that skips the signature check, for replaying saved bodies during
    # development only. Never put such a router behind a reachable endpoint:
    # anyone could send it a delivery.
    def self.for_unsigned_replay(store: nil)
      router = allocate
      router.send(:setup, nil, store)
      router
    end

    # Registers the block for each of the triggers. Every trigger must have a
    # delivery model in WebhookTriggers; for the others use on_other. Returns the
    # lambda that removes exactly this registration.
    def on(*triggers, &handler)
      raise ArgumentError, 'a handler block is required' unless handler

      triggers = triggers.flatten.map(&:to_s)
      raise ArgumentError, 'at least one trigger is required' if triggers.empty?

      triggers.each do |trigger|
        next if WebhookTriggers.delivery_type(trigger)

        raise ArgumentError, "'#{trigger}' has no delivery model in this SDK; register it with on_other"
      end

      @lock.synchronize do
        handlers = @handlers.dup
        triggers.each { |t| handlers[t] = [*handlers[t], handler].freeze }
        @handlers = handlers.freeze
      end
      -> { off(triggers, handler) }
    end

    # Removes the handler from each of the triggers.
    def off(triggers, handler)
      triggers = Array(triggers).map(&:to_s)
      @lock.synchronize do
        handlers = @handlers.dup
        triggers.each do |trigger|
          left = (handlers[trigger] || []).reject { |h| h.equal?(handler) }
          left.empty? ? handlers.delete(trigger) : handlers[trigger] = left.freeze
        end
        @handlers = handlers.freeze
      end
      nil
    end

    # Registers the block for the deliveries no other handler takes: triggers
    # without a registration, the untyped catch-all '*', and triggers added after
    # this SDK was built. It gets an OtherWebhook. Returns the lambda that removes
    # it.
    def on_other(&handler)
      raise ArgumentError, 'a handler block is required' unless handler

      @lock.synchronize { @other_handlers = [*@other_handlers, handler].freeze }
      lambda do
        @lock.synchronize { @other_handlers = @other_handlers.reject { |h| h.equal?(handler) }.freeze }
        nil
      end
    end

    # Handles one delivery.
    #
    # body: the request body, exactly as received (String, read binary).
    # signature: the x-docspace-signature-256 header.
    # event_id_header: the x-docspace-event-id header, if at hand: an already
    #   processed id is dropped before any work. It is not signed, so it is only
    #   ever used to drop a delivery, never to accept one.
    def handle(body, signature, event_id_header = nil)
      body = body.to_s
      header_id = event_id_header.is_a?(String) ? Integer(event_id_header.strip, 10, exception: false) : nil
      if @store && header_id && @store.processed?(header_id)
        return WebhookHandleResult.new(outcome: :duplicate, status_code: 200, event_id: header_id)
      end

      if @secret && !self.class.verify_signature(body, @secret, signature)
        return WebhookHandleResult.new(outcome: :rejected, status_code: 401,
                                       error: 'signature is missing or does not match')
      end

      begin
        envelope, trigger, delivery, event = parse(body)
      rescue ParseError => e
        return WebhookHandleResult.new(outcome: :invalid, status_code: 400, error: e.message, cause: e)
      end

      event_id = event&.id

      if @store && !event_id.nil? && @store.processed?(event_id)
        return WebhookHandleResult.new(outcome: :duplicate, status_code: 200, trigger: trigger, event_id: event_id)
      end

      # Snapshots: a registration made while handling does not touch this call.
      typed = delivery ? (@handlers[trigger] || []) : []
      other = typed.empty? ? @other_handlers : []

      begin
        typed.each { |handler| handler.call(delivery, trigger) }
        unless other.empty?
          hook = OtherWebhook.new(trigger: trigger, event: event,
                                  webhook: WebhookConfigInfo.build_from_hash(envelope['webhook']),
                                  payload: envelope['payload'], raw: envelope)
          other.each { |handler| handler.call(hook) }
        end
      rescue StandardError => e
        return WebhookHandleResult.new(outcome: :failed, status_code: 500, trigger: trigger,
                                       event_id: event_id, error: e.message, cause: e)
      end

      @store.mark_processed(event_id) if @store && !event_id.nil?

      WebhookHandleResult.new(outcome: (typed.size + other.size).positive? ? :handled : :unhandled,
                              status_code: 200, trigger: trigger, event_id: event_id)
    end

    # `sha256=` + UPPERCASE hex HMAC-SHA256 of the raw body, as DocSpace sends it.
    def self.compute_signature(body, secret)
      "sha256=#{OpenSSL::HMAC.hexdigest('SHA256', secret, body.to_s).upcase}"
    end

    # Constant-time, case-insensitive check over the RAW body. DocSpace emits
    # uppercase hex where GitHub emits lowercase, so the comparison folds case.
    # A missing or malformed header is false, never an exception.
    def self.verify_signature(body, secret, signature)
      return false unless signature.is_a?(String)

      signature = signature.strip
      return false if signature.empty?

      # secure_compare hashes both sides first, so unequal lengths are fine.
      OpenSSL.secure_compare(compute_signature(body, secret).downcase, signature.downcase)
    end

    # The body is not a delivery.
    class ParseError < StandardError; end

    private

    def setup(secret, store)
      @secret = secret
      @store = store
      @handlers = {}.freeze
      @other_handlers = [].freeze
      @lock = Mutex.new
    end

    # Returns [envelope Hash, trigger, delivery model or nil, WebhookEventInfo].
    def parse(body)
      begin
        envelope = JSON.parse(body)
      rescue JSON::ParserError => e
        raise ParseError, "body is not JSON: #{e.message}"
      end
      raise ParseError, 'body is not a JSON object' unless envelope.is_a?(Hash)

      event = envelope['event']
      trigger = event.is_a?(Hash) ? event['trigger'] : nil
      raise ParseError, 'envelope has no event.trigger' unless trigger.is_a?(String) && !trigger.empty?

      # The catch-all, or a trigger added on the server after this SDK was
      # generated, stays opaque and is never parsed with some other model.
      model = WebhookTriggers.delivery_type(trigger)
      unless model
        begin
          return [envelope, trigger, nil, WebhookEventInfo.build_from_hash(event)]
        rescue StandardError => e
          raise ParseError, "unparseable #{trigger} event: #{e.message}"
        end
      end

      # A typed trigger always carries its payload object; the generated
      # build_from_hash passes nil through, which would hand the handler a
      # delivery without its payload.
      unless envelope['payload'].is_a?(Hash)
        raise ParseError, "no #{WebhookTriggers::PAYLOAD_TYPES[trigger]} payload for '#{trigger}'"
      end

      begin
        delivery = model.build_from_hash(envelope)
      rescue StandardError => e
        raise ParseError, "unparseable #{trigger} delivery: #{e.message}"
      end
      raise ParseError, "unparseable #{trigger} delivery: not a #{model.name}" unless delivery.is_a?(model)

      [envelope, trigger, delivery, delivery.event]
    end
  end
end
