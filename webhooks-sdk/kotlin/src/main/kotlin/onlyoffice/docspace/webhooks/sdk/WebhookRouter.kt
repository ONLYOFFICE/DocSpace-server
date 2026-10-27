package onlyoffice.docspace.webhooks.sdk

import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicReference

/** What [WebhookRouter.handle] did with a delivery. */
enum class WebhookOutcome {
    /** At least one handler ran, and all of them completed. */
    HANDLED,

    /** The event was processed before; nothing ran. */
    DUPLICATE,

    /** No handler is registered for the trigger; the delivery is acknowledged all the same. */
    UNHANDLED,

    /** The signature is missing or does not match. */
    REJECTED,

    /** The body is not a delivery, or a typed trigger arrived without its payload. */
    INVALID,

    /** A handler threw; the remaining ones did not run. */
    FAILED,
}

/**
 * The result of one delivery: what happened and the HTTP status to answer DocSpace with. A 2xx acknowledges the
 * delivery; anything else is a failed attempt, which DocSpace retries on its schedule.
 */
data class WebhookHandleResult(
    val outcome: WebhookOutcome,
    val statusCode: Int,
    val trigger: String? = null,
    val eventId: Int? = null,
    val error: String? = null,
    val cause: Throwable? = null,
)

/**
 * Remembers which events were processed. DocSpace delivers at least once: an automatic or manual retry sends the
 * same `event.id` again, and the signature does not prove when a request was sent, so the id is the duplicate and
 * replay defence. A real receiver persists the ids for as long as an event can still arrive -- the automatic
 * retries span about 18 hours on the default schedule, and a manual retry can come at any time later.
 */
interface WebhookEventStore {
    /** True when the event was processed before. */
    fun isProcessed(eventId: Int): Boolean

    /** Records the event as processed; called only after every handler completed. */
    fun markProcessed(eventId: Int)
}

/** A [WebhookEventStore] in process memory: for samples and tests, lost on restart. */
class InMemoryWebhookEventStore : WebhookEventStore {
    private val ids: MutableSet<Int> = ConcurrentHashMap.newKeySet()

    override fun isProcessed(eventId: Int): Boolean = eventId in ids

    override fun markProcessed(eventId: Int) {
        ids.add(eventId)
    }
}

/** One handler registration; closing it removes exactly that registration, and closing it again does nothing. */
fun interface WebhookRegistration : AutoCloseable {
    override fun close()
}

/**
 * Dispatches verified deliveries to handlers registered per trigger (`onFileCreated`, ... or [on]), or for
 * anything else ([onOther]). Any number of handlers can be registered for one trigger; each registration returns
 * a [WebhookRegistration] that removes it, and [off] does the same.
 *
 * [handle] checks the signature, drops a duplicate, reads the body into the delivery model of its trigger and
 * calls every handler of that trigger in registration order, each to completion before the next. Answer DocSpace
 * with the result's [WebhookHandleResult.statusCode] only after it returns: a handler that throws makes it a
 * failed attempt, and DocSpace sends the delivery again, so handlers have to be idempotent.
 */
class WebhookRouter private constructor(
    private val store: WebhookEventStore?,
    private val secret: String?,
) {
    /**
     * A router that accepts only deliveries signed with [secret], the secret key of the subscription. Without a
     * [store], where processed event ids are kept, duplicates are not detected.
     */
    @JvmOverloads
    constructor(secret: String, store: WebhookEventStore? = null) : this(store, requireSecret(secret))

    private val lock = Any()

    // copy-on-write: a delivery being handled keeps the snapshot it started with
    @Volatile
    private var byTrigger: Map<String, List<Registration>> = emptyMap()

    @Volatile
    private var other: List<Registration> = emptyList()

    /**
     * Registers [handler] for [trigger], which has to be delivered as [type]. The generated `on<Trigger>`
     * extensions call this.
     *
     * @throws IllegalArgumentException the trigger is not delivered as [type].
     */
    fun <T : Any> on(trigger: String, type: Class<T>, handler: (T) -> Unit): WebhookRegistration {
        val expected = WebhookTriggers.deliveryType(trigger)
        require(expected == type) {
            "'$trigger' is delivered as ${expected?.simpleName ?: "an untyped body"}, not ${type.simpleName}"
        }

        val registration = Registration(handler) { delivery, _ -> handler(type.cast(delivery)) }

        synchronized(lock) {
            byTrigger = byTrigger + (trigger to (byTrigger[trigger].orEmpty() + registration))
        }

        return unsubscriber {
            synchronized(lock) {
                byTrigger = without(byTrigger, trigger) { it !== registration }
            }
        }
    }

    /** Removes every registration of [handler] for [trigger]. */
    fun off(trigger: String, handler: (Nothing) -> Unit) {
        synchronized(lock) {
            byTrigger = without(byTrigger, trigger) { it.handler !== handler }
        }
    }

    /**
     * Registers [handler] for the deliveries no other handler takes: triggers without a registration, the untyped
     * catch-all `*`, and triggers added after this SDK was built. It gets the envelope with the payload as
     * [WebhookParser.parse] reads it.
     */
    fun onOther(handler: (DocSpaceWebhook) -> Unit): WebhookRegistration {
        val registration = Registration(handler) { _, hook -> handler(hook) }

        synchronized(lock) {
            other = other + registration
        }

        return unsubscriber {
            synchronized(lock) {
                other = other.filter { it !== registration }
            }
        }
    }

    /**
     * Handles one delivery; see [WebhookRouter].
     *
     * @param body the request body, exactly as received.
     * @param signature the `x-docspace-signature-256` header.
     * @param eventIdHeader the `x-docspace-event-id` header, if at hand: an already processed id is dropped before
     *   any work. The header is not signed, so it is only ever used to drop a delivery, never to accept one.
     */
    @JvmOverloads
    fun handle(body: ByteArray, signature: String?, eventIdHeader: String? = null): WebhookHandleResult {
        val headerId = eventIdHeader?.trim()?.toIntOrNull()
        if (store != null && headerId != null && store.isProcessed(headerId)) {
            return WebhookHandleResult(WebhookOutcome.DUPLICATE, 200, eventId = headerId)
        }

        if (secret != null && !WebhookSignature.verify(body, secret, signature)) {
            return WebhookHandleResult(WebhookOutcome.REJECTED, 401, error = "signature is missing or does not match")
        }

        val hook: DocSpaceWebhook
        val delivery: Any?
        try {
            hook = WebhookParser.parse(body)
            delivery = WebhookParser.parseDelivery(body)
        } catch (e: WebhookParseException) {
            return WebhookHandleResult(WebhookOutcome.INVALID, 400, error = e.message, cause = e)
        }

        val eventId = hook.event?.id

        if (store != null && eventId != null && store.isProcessed(eventId)) {
            return WebhookHandleResult(WebhookOutcome.DUPLICATE, 200, hook.trigger, eventId)
        }

        val handlers = handlersFor(hook.trigger, delivery)

        for (handler in handlers) {
            try {
                handler.invoke(delivery, hook)
            } catch (e: Exception) {
                return WebhookHandleResult(
                    WebhookOutcome.FAILED, 500, hook.trigger, eventId, e.message ?: e.toString(), e,
                )
            }
        }

        if (store != null && eventId != null) {
            store.markProcessed(eventId)
        }

        return WebhookHandleResult(
            if (handlers.isNotEmpty()) WebhookOutcome.HANDLED else WebhookOutcome.UNHANDLED,
            200,
            hook.trigger,
            eventId,
        )
    }

    /** [handle] over a body already decoded to text; it is signed as its UTF-8 bytes. */
    @JvmOverloads
    fun handle(body: String, signature: String?, eventIdHeader: String? = null): WebhookHandleResult =
        handle(body.toByteArray(Charsets.UTF_8), signature, eventIdHeader)

    private fun handlersFor(trigger: String, delivery: Any?): List<Registration> {
        // one consistent snapshot: registrations are replaced, never changed in place
        val (typed, rest) = synchronized(lock) { byTrigger to other }

        if (delivery == null) {
            return rest
        }

        return typed[trigger].orEmpty().ifEmpty { rest }
    }

    private class Registration(
        val handler: Any,
        val invoke: (delivery: Any?, hook: DocSpaceWebhook) -> Unit,
    )

    companion object {
        /**
         * A router that does not check signatures, for replaying saved bodies during development. Never put it
         * behind a reachable endpoint: anyone could send it a delivery.
         */
        @JvmStatic
        @JvmOverloads
        fun forUnsignedReplay(store: WebhookEventStore? = null): WebhookRouter = WebhookRouter(store, null)

        private fun requireSecret(secret: String): String {
            require(secret.isNotEmpty()) { "a secret is required; use forUnsignedReplay for unsigned bodies" }
            return secret
        }

        private fun without(
            source: Map<String, List<Registration>>,
            trigger: String,
            keep: (Registration) -> Boolean,
        ): Map<String, List<Registration>> {
            val left = source[trigger].orEmpty().filter(keep)
            return if (left.isNotEmpty()) source + (trigger to left) else source - trigger
        }

        private fun unsubscriber(remove: () -> Unit): WebhookRegistration {
            val pending = AtomicReference(remove)
            return WebhookRegistration { pending.getAndSet(null)?.invoke() }
        }
    }
}
