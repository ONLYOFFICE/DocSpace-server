// Dispatch of verified deliveries to handlers. Hand-written in swift/shim/ and
// copied into generated/Sources/DocSpaceWebhooksSDK/ after every regeneration;
// the typed onFileCreated, ... methods are generated into WebhookTriggers.swift.

import Foundation

/// What `WebhookRouter.handle(body:signature:eventIdHeader:)` did with a delivery.
public enum WebhookOutcome: String, Sendable {
    /// At least one handler ran, and all of them completed.
    case handled
    /// The event was processed before; nothing ran.
    case duplicate
    /// No handler is registered for the trigger; the delivery is acknowledged all the same.
    case unhandled
    /// The signature is missing or does not match.
    case rejected
    /// The body is not a delivery, or a typed trigger arrived without its payload.
    case invalid
    /// A handler (or the event store) threw; the remaining handlers did not run.
    case failed
}

/// The result of one delivery: what happened and the HTTP status to answer DocSpace with. A 2xx acknowledges the
/// delivery; anything else is a failed attempt, which DocSpace retries on its schedule.
public struct WebhookHandleResult: Sendable {
    public let outcome: WebhookOutcome
    public let statusCode: Int
    public let trigger: String?
    public let eventId: Int?
    public let error: String?
    public let cause: (any Error)?

    public init(
        outcome: WebhookOutcome,
        statusCode: Int,
        trigger: String? = nil,
        eventId: Int? = nil,
        error: String? = nil,
        cause: (any Error)? = nil
    ) {
        self.outcome = outcome
        self.statusCode = statusCode
        self.trigger = trigger
        self.eventId = eventId
        self.error = error
        self.cause = cause
    }
}

/// Remembers which events were processed. DocSpace delivers at least once: an automatic or manual retry sends the
/// same `event.id` again, and the signature does not prove when a request was sent, so the id is the duplicate and
/// replay defence. A real receiver persists the ids for as long as an event can still arrive -- the automatic
/// retries span about 18 hours on the default schedule, and a manual retry can come at any time later.
///
/// An error thrown by the store makes the delivery `failed` (500), so DocSpace sends it again.
public protocol WebhookEventStore: Sendable {
    /// True when the event was processed before.
    func isProcessed(_ eventId: Int) async throws -> Bool

    /// Records the event as processed; called only after every handler completed.
    func markProcessed(_ eventId: Int) async throws
}

/// A `WebhookEventStore` in process memory: for samples and tests, lost on restart.
public final class InMemoryWebhookEventStore: WebhookEventStore, @unchecked Sendable {
    private let lock = NSLock()
    private var ids = Set<Int>()

    public init() {}

    public func isProcessed(_ eventId: Int) async throws -> Bool {
        synchronized { $0.contains(eventId) }
    }

    public func markProcessed(_ eventId: Int) async throws {
        synchronized { _ = $0.insert(eventId) }
    }

    // NSLock may not be taken directly in an async function; a synchronous helper is fine
    private func synchronized<R>(_ body: (inout Set<Int>) -> R) -> R {
        lock.lock()
        defer { lock.unlock() }
        return body(&ids)
    }
}

/// A delivery as the "other" handlers get it: the envelope with the payload left as JSON.
public struct DocSpaceWebhook: Sendable {
    /// `event.trigger`: `*` for the legacy catch-all, or a trigger this build may not know.
    public let trigger: String
    public let event: WebhookEventInfo?
    public let webhook: WebhookConfigInfo?
    /// The payload as received, not narrowed to a model.
    public let payload: JSONValue?

    /// True when the trigger is in the contract this build was generated from, the catch-all included.
    public var isKnownTrigger: Bool { WebhookTriggers.isKnown(trigger) }
}

/// One registration with a `WebhookRouter`; `cancel()` removes exactly that handler. Dropping the value does NOT
/// remove it, so a handler registered for the router's lifetime needs no bookkeeping.
public final class WebhookRegistration: @unchecked Sendable {
    private let lock = NSLock()
    private var remove: (() -> Void)?

    init(_ remove: @escaping () -> Void) {
        self.remove = remove
    }

    /// Removes the handler; later calls do nothing.
    public func cancel() {
        lock.lock()
        let remove = self.remove
        self.remove = nil
        lock.unlock()

        remove?()
    }
}

/// Errors of `WebhookRouter` construction.
public enum WebhookRouterError: Error, CustomStringConvertible {
    /// The secret is empty: every delivery would be rejected, or, worse, checked against an empty key.
    case emptySecret

    public var description: String {
        switch self {
        case .emptySecret: return "the webhook secret is empty"
        }
    }
}

/// Dispatches verified deliveries to handlers registered per trigger (`onFileCreated`, ..., or the generic
/// `on(_:_:_:)` they call), or for anything else (`onOther`). Any number of handlers can be registered for one
/// trigger; each registration returns a `WebhookRegistration` that removes it.
///
/// `handle(body:signature:eventIdHeader:)` checks the signature, drops a duplicate, reads the body into the delivery
/// model of its trigger and awaits every handler of that trigger in registration order. Answer DocSpace with the
/// result's `statusCode` only after it returns: a handler that throws makes it a failed attempt, and DocSpace sends
/// the delivery again, so handlers have to be idempotent.
public final class WebhookRouter: @unchecked Sendable {
    private typealias Invoke = @Sendable (Any?, DocSpaceWebhook) async throws -> Void

    private final class Entry: @unchecked Sendable {
        let invoke: Invoke

        init(_ invoke: @escaping Invoke) {
            self.invoke = invoke
        }
    }

    private let secret: String?
    private let store: WebhookEventStore?
    private let lock = NSLock()

    // replaced under the lock, never changed in place, so a snapshot stays consistent while handlers run
    private var byTrigger: [String: [Entry]] = [:]
    private var other: [Entry] = []

    /// A router that accepts only deliveries signed with `secret`.
    /// - Parameters:
    ///   - secret: The secret key of the subscription; must not be empty.
    ///   - store: Where processed event ids are kept; without one, duplicates are not detected.
    public init(secret: String, store: WebhookEventStore? = nil) throws {
        guard !secret.isEmpty else { throw WebhookRouterError.emptySecret }

        self.secret = secret
        self.store = store
    }

    private init(unsignedWith store: WebhookEventStore?) {
        self.secret = nil
        self.store = store
    }

    /// A router that does not check signatures, for replaying saved bodies during development only. Never put it
    /// behind a reachable endpoint: anyone could send it a delivery.
    public static func forUnsignedReplay(store: WebhookEventStore? = nil) -> WebhookRouter {
        WebhookRouter(unsignedWith: store)
    }

    /// Registers `handler` for `trigger`, which has to be delivered as `T`. The generated `on<Trigger>` methods call
    /// this.
    ///
    /// A trigger without a delivery model (the catch-all `*`, a name not in the contract) or a `T` other than the one
    /// `WebhookTriggers.deliveryModels` names is a programming error and stops the process (`preconditionFailure`);
    /// the generated methods always match.
    @discardableResult
    public func on<T: Decodable>(
        _ trigger: String,
        _ type: T.Type,
        _ handler: @escaping @Sendable (T) async throws -> Void
    ) -> WebhookRegistration {
        let model = WebhookTriggers.deliveryModels[trigger]
        guard model == String(describing: T.self) else {
            preconditionFailure("'\(trigger)' is delivered as \(model ?? "an untyped body"), not \(T.self)")
        }

        let entry = Entry { delivery, _ in
            // decodeDelivery picked the model by the same table, so the cast cannot fail
            guard let delivery = delivery as? T else { return }
            try await handler(delivery)
        }

        lock.lock()
        byTrigger[trigger, default: []].append(entry)
        lock.unlock()

        return WebhookRegistration { [weak self] in
            guard let self else { return }
            self.lock.lock()
            defer { self.lock.unlock() }

            let left = (self.byTrigger[trigger] ?? []).filter { $0 !== entry }
            self.byTrigger[trigger] = left.isEmpty ? nil : left
        }
    }

    /// Registers `handler` for the deliveries no other handler takes: triggers without a registration, the untyped
    /// catch-all `*`, and triggers added after this SDK was built. It gets the envelope with the raw payload.
    @discardableResult
    public func onOther(_ handler: @escaping @Sendable (DocSpaceWebhook) async throws -> Void) -> WebhookRegistration {
        let entry = Entry { _, hook in try await handler(hook) }

        lock.lock()
        other.append(entry)
        lock.unlock()

        return WebhookRegistration { [weak self] in
            guard let self else { return }
            self.lock.lock()
            defer { self.lock.unlock() }

            self.other = self.other.filter { $0 !== entry }
        }
    }

    /// Handles one delivery; see the documentation of `WebhookRouter`.
    /// - Parameters:
    ///   - body: The request body, exactly as received.
    ///   - signature: The `x-docspace-signature-256` header.
    ///   - eventIdHeader: The `x-docspace-event-id` header, if at hand: an already processed id is dropped before
    ///     any work. The header is not signed, so it is only ever used to drop a delivery, never to accept one.
    public func handle(body: Data, signature: String?, eventIdHeader: String? = nil) async -> WebhookHandleResult {
        if let store,
           let headerId = eventIdHeader.flatMap({ Int($0.trimmingCharacters(in: .whitespaces)) }) {
            do {
                if try await store.isProcessed(headerId) {
                    return WebhookHandleResult(outcome: .duplicate, statusCode: 200, eventId: headerId)
                }
            } catch {
                return failed(error, trigger: nil, eventId: headerId)
            }
        }

        if let secret, !WebhookSignature.verify(body: body, secret: secret, signature: signature) {
            return WebhookHandleResult(
                outcome: .rejected, statusCode: 401, error: "signature is missing or does not match")
        }

        let hook: DocSpaceWebhook
        let delivery: Any?
        do {
            (hook, delivery) = try Self.parse(body)
        } catch {
            return WebhookHandleResult(outcome: .invalid, statusCode: 400, error: Self.message(error), cause: error)
        }

        let eventId = hook.event?.id

        if let store, let eventId {
            do {
                if try await store.isProcessed(eventId) {
                    return WebhookHandleResult(outcome: .duplicate, statusCode: 200, trigger: hook.trigger, eventId: eventId)
                }
            } catch {
                return failed(error, trigger: hook.trigger, eventId: eventId)
            }
        }

        let handlers = handlers(for: hook.trigger, typed: delivery != nil)

        for handler in handlers {
            do {
                try await handler.invoke(delivery, hook)
            } catch {
                return failed(error, trigger: hook.trigger, eventId: eventId)
            }
        }

        if let store, let eventId {
            do {
                try await store.markProcessed(eventId)
            } catch {
                return failed(error, trigger: hook.trigger, eventId: eventId)
            }
        }

        return WebhookHandleResult(
            outcome: handlers.isEmpty ? .unhandled : .handled, statusCode: 200, trigger: hook.trigger, eventId: eventId)
    }

    /// Handles one delivery whose body is at hand as a string; it is checked as its UTF-8 bytes.
    public func handle(body: String, signature: String?, eventIdHeader: String? = nil) async -> WebhookHandleResult {
        await handle(body: Data(body.utf8), signature: signature, eventIdHeader: eventIdHeader)
    }

    private func handlers(for trigger: String, typed: Bool) -> [Entry] {
        lock.lock()
        let (byTrigger, other) = (self.byTrigger, self.other)
        lock.unlock()

        guard typed else { return other }

        let own = byTrigger[trigger] ?? []
        return own.isEmpty ? other : own
    }

    private func failed(_ error: any Error, trigger: String?, eventId: Int?) -> WebhookHandleResult {
        WebhookHandleResult(
            outcome: .failed, statusCode: 500, trigger: trigger, eventId: eventId, error: Self.message(error), cause: error)
    }

    // ---------------------------------------------------------------- parsing

    /// Why a body is not a delivery.
    public struct ParseError: Error, CustomStringConvertible {
        public let description: String
    }

    /// The envelope with the payload left as JSON, plus the whole body read into the delivery model of its trigger
    /// (nil when the trigger has none).
    private static func parse(_ body: Data) throws -> (DocSpaceWebhook, Any?) {
        let json: Any
        do {
            json = try JSONSerialization.jsonObject(with: body)
        } catch {
            throw ParseError(description: "body is not JSON: \(message(error))")
        }

        guard let root = json as? [String: Any] else {
            throw ParseError(description: "body is not a webhook envelope")
        }
        guard let trigger = (root["event"] as? [String: Any])?["trigger"] as? String else {
            throw ParseError(description: "envelope has no event.trigger")
        }

        let decoder = JSONDecoder()
        // The contract emits UTC ISO-8601; Date properties will not decode without this.
        decoder.dateDecodingStrategy = .iso8601

        let envelope: WebhookEnvelope
        do {
            envelope = try decoder.decode(WebhookEnvelope.self, from: body)
        } catch {
            throw ParseError(description: "unparseable envelope: \(message(error))")
        }

        let hook = DocSpaceWebhook(
            trigger: trigger, event: envelope.event, webhook: envelope.webhook, payload: envelope.payload)

        guard let model = WebhookTriggers.deliveryModels[trigger] else {
            // the catch-all, or a trigger newer than this SDK: acknowledged with its raw payload
            return (hook, nil)
        }

        // A typed trigger always carries its payload object; the generated models would take a missing one as nil.
        guard root["payload"] is [String: Any] else {
            throw ParseError(description: "no payload object for '\(trigger)'")
        }

        do {
            return (hook, try WebhookTriggers.decodeDelivery(trigger: trigger, from: body, decoder: decoder))
        } catch {
            throw ParseError(description: "unparseable \(model) for '\(trigger)': \(message(error))")
        }
    }

    private static func message(_ error: any Error) -> String {
        (error as? LocalizedError)?.errorDescription ?? String(describing: error)
    }
}
