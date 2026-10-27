import type { TriggerDeliveryMap, TypedTriggerName } from '../generated/triggers';
import { AnyWebhook, WebhookParseError, parseDelivery, parseWebhook } from './parse';
import { RawBody, verifySignature } from './verify';

/** What `WebhookRouter.handle` did with a delivery. */
export type WebhookOutcome =
    /** At least one handler ran, and all of them completed. */
    | 'handled'
    /** The event was processed before; nothing ran. */
    | 'duplicate'
    /** No handler is registered for the trigger; the delivery is acknowledged all the same. */
    | 'unhandled'
    /** The signature is missing or does not match. */
    | 'rejected'
    /** The body is not a delivery, or a typed trigger arrived without its payload. */
    | 'invalid'
    /** A handler threw; the remaining ones did not run. */
    | 'failed';

/**
 * The result of one delivery: what happened and the HTTP status to answer
 * DocSpace with. A 2xx acknowledges the delivery; anything else is a failed
 * attempt, which DocSpace retries on its schedule.
 */
export interface WebhookHandleResult {
    outcome: WebhookOutcome;
    statusCode: number;
    trigger?: string;
    eventId?: number;
    error?: string;
    cause?: unknown;
}

/**
 * Remembers which events were processed. DocSpace delivers at least once: an
 * automatic or manual retry sends the same `event.id` again, and the signature
 * does not prove when a request was sent, so the id is the duplicate and replay
 * defence. A real receiver persists the ids for as long as an event can still
 * arrive -- the automatic retries span about 18 hours on the default schedule,
 * and a manual retry can come at any time later.
 */
export interface WebhookEventStore {
    isProcessed(eventId: number): boolean | Promise<boolean>;
    /** Called only after every handler completed. */
    markProcessed(eventId: number): void | Promise<void>;
}

/** A `WebhookEventStore` in process memory: for samples and tests, lost on restart. */
export class InMemoryWebhookEventStore implements WebhookEventStore {
    private readonly ids = new Set<number>();

    isProcessed(eventId: number): boolean {
        return this.ids.has(eventId);
    }

    markProcessed(eventId: number): void {
        this.ids.add(eventId);
    }
}

/** A handler of one trigger; it may return a promise, which the router awaits. */
export type DeliveryHandler<K extends TypedTriggerName> =
    (delivery: TriggerDeliveryMap[K], trigger: K) => void | Promise<void>;

/** A handler of the deliveries no other handler takes. */
export type OtherHandler = (webhook: AnyWebhook) => void | Promise<void>;

export interface WebhookRouterOptions {
    /** The secret key of the subscription. Required unless `unsignedReplay` is set. */
    secret?: string;
    /** Where processed event ids are kept; without one, duplicates are not detected. */
    store?: WebhookEventStore;
    /**
     * Skip the signature check, for replaying saved bodies during development.
     * Never put such a router behind a reachable endpoint: anyone could send it
     * a delivery.
     */
    unsignedReplay?: boolean;
}

type AnyHandler = (delivery: any, trigger: any) => void | Promise<void>;

/**
 * Dispatches verified deliveries to handlers registered per trigger. The
 * handler's argument is typed by the trigger: `on('file.created', d => ...)`
 * gets a `FileDelivery`. Any number of handlers can be registered for one
 * trigger; `on` returns a function that removes the handler again, and `off`
 * does the same.
 *
 * `handle` checks the signature, drops a duplicate, reads the body into the
 * delivery model of its trigger and awaits every handler of that trigger in
 * registration order. Answer DocSpace with the result's `statusCode` only after
 * it resolves: a handler that throws makes it a failed attempt, and DocSpace
 * sends the delivery again, so handlers have to be idempotent.
 */
export class WebhookRouter {
    private readonly secret: string | undefined;
    private readonly store: WebhookEventStore | undefined;
    private readonly handlers = new Map<string, AnyHandler[]>();
    private otherHandlers: OtherHandler[] = [];

    constructor(options: WebhookRouterOptions) {
        if (!options.unsignedReplay && !options.secret) {
            throw new Error('a secret is required unless unsignedReplay is set');
        }
        this.secret = options.unsignedReplay ? undefined : options.secret;
        this.store = options.store;
    }

    /**
     * Registers `handler` for each of `triggers`; with several triggers the
     * argument is the union of their delivery models. Returns the function that
     * removes it.
     */
    on<K extends TypedTriggerName>(triggers: K | readonly K[], handler: DeliveryHandler<K>): () => void {
        const list = (Array.isArray(triggers) ? triggers : [triggers]) as K[];
        for (const trigger of list) {
            this.handlers.set(trigger, [...(this.handlers.get(trigger) ?? []), handler as AnyHandler]);
        }
        return () => this.off(list, handler);
    }

    /** Removes `handler` from each of `triggers`. */
    off<K extends TypedTriggerName>(triggers: K | readonly K[], handler: DeliveryHandler<K>): void {
        const list = (Array.isArray(triggers) ? triggers : [triggers]) as K[];
        for (const trigger of list) {
            const left = (this.handlers.get(trigger) ?? []).filter(h => h !== handler);
            if (left.length > 0) {
                this.handlers.set(trigger, left);
            } else {
                this.handlers.delete(trigger);
            }
        }
    }

    /**
     * Registers `handler` for the deliveries no other handler takes: triggers
     * without a registration, the untyped catch-all `'*'`, and triggers added
     * after this SDK was built. Returns the function that removes it.
     */
    onOther(handler: OtherHandler): () => void {
        this.otherHandlers = [...this.otherHandlers, handler];
        return () => {
            this.otherHandlers = this.otherHandlers.filter(h => h !== handler);
        };
    }

    /**
     * Handles one delivery.
     *
     * @param body The request body, exactly as received.
     * @param signature The `x-docspace-signature-256` header.
     * @param eventIdHeader The `x-docspace-event-id` header, if at hand: an
     *   already processed id is dropped before any work. It is not signed, so it
     *   is only ever used to drop a delivery, never to accept one.
     */
    async handle(
        body: RawBody,
        signature: string | string[] | undefined | null,
        eventIdHeader?: string | null,
    ): Promise<WebhookHandleResult> {
        const headerId = eventIdHeader ? Number(eventIdHeader) : NaN;
        if (this.store && Number.isInteger(headerId) && await this.store.isProcessed(headerId)) {
            return { outcome: 'duplicate', statusCode: 200, eventId: headerId };
        }

        if (this.secret !== undefined && !verifySignature(body, this.secret, signature)) {
            return { outcome: 'rejected', statusCode: 401, error: 'signature is missing or does not match' };
        }

        let webhook: AnyWebhook;
        let delivery: ReturnType<typeof parseDelivery>;
        try {
            webhook = parseWebhook(body);
            delivery = parseDelivery(body);
        } catch (err) {
            if (err instanceof WebhookParseError) {
                return { outcome: 'invalid', statusCode: 400, error: err.message, cause: err };
            }
            throw err;
        }

        const { trigger } = webhook;
        const eventId = webhook.event.id;

        if (this.store && eventId !== undefined && await this.store.isProcessed(eventId)) {
            return { outcome: 'duplicate', statusCode: 200, trigger, eventId };
        }

        const typed = delivery ? (this.handlers.get(delivery.trigger) ?? []) : [];
        const other = typed.length === 0 ? this.otherHandlers : [];

        try {
            for (const handler of typed) {
                await handler(delivery!.delivery, delivery!.trigger);
            }
            for (const handler of other) {
                await handler(webhook);
            }
        } catch (err) {
            return {
                outcome: 'failed',
                statusCode: 500,
                trigger,
                eventId,
                error: err instanceof Error ? err.message : String(err),
                cause: err,
            };
        }

        if (this.store && eventId !== undefined) {
            await this.store.markProcessed(eventId);
        }

        return {
            outcome: typed.length + other.length > 0 ? 'handled' : 'unhandled',
            statusCode: 200,
            trigger,
            eventId,
        };
    }
}
