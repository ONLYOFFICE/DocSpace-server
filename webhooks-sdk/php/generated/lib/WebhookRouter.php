<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEnvelope;

/**
 * Dispatches verified deliveries to handlers registered per trigger. A handler
 * of a trigger gets the delivery model the generated WebhookTriggers table
 * names for it (`FileDelivery` for `file.created`, ...) and the trigger:
 *
 *     $router = new WebhookRouter($secret, $store);
 *     $router->on('file.created', function (FileDelivery $d, string $trigger): void { ... });
 *     $result = $router->handle(file_get_contents('php://input'),
 *         $_SERVER['HTTP_X_DOCSPACE_SIGNATURE_256'] ?? null,
 *         $_SERVER['HTTP_X_DOCSPACE_EVENT_ID'] ?? null);
 *     http_response_code($result->statusCode);
 *
 * Any number of handlers can be registered for one trigger; on() returns a
 * closure that removes that registration again, and off() removes a handler.
 *
 * handle() checks the signature, drops a duplicate, reads the body into the
 * delivery model of its trigger and calls every handler of that trigger in
 * registration order. Answer DocSpace with the result's statusCode only after
 * it returns: a handler that throws makes it a failed attempt, and DocSpace
 * sends the delivery again, so handlers have to be idempotent.
 */
final class WebhookRouter
{
    private ?string $secret = null;
    private ?WebhookEventStore $store = null;

    /** @var array<string, array<int, callable>> trigger => registration id => handler */
    private array $handlers = [];

    /** @var array<int, callable> registration id => handler */
    private array $otherHandlers = [];

    private int $nextId = 0;

    /**
     * A router that accepts only deliveries signed with $secret, the secret key
     * of the subscription. Without a $store, duplicates are not detected.
     *
     * @throws \InvalidArgumentException The secret is empty.
     */
    public function __construct(string $secret, ?WebhookEventStore $store = null)
    {
        if ($secret === '') {
            throw new \InvalidArgumentException(
                'a secret is required; WebhookRouter::forUnsignedReplay() is the router without one');
        }
        $this->secret = $secret;
        $this->store = $store;
    }

    /**
     * A router that does not check signatures, for replaying saved bodies during
     * development only. Never put it behind a reachable endpoint: anyone could
     * send it a delivery.
     */
    public static function forUnsignedReplay(?WebhookEventStore $store = null): self
    {
        $router = (new \ReflectionClass(self::class))->newInstanceWithoutConstructor();
        $router->store = $store;
        return $router;
    }

    /**
     * Registers $handler for each of $triggers. It is called as
     * `$handler($delivery, $trigger)`, with the delivery model of the trigger.
     * Returns the closure that removes this registration.
     *
     * @param string|string[] $triggers
     * @throws \InvalidArgumentException A trigger has no delivery model: the
     *   catch-all `*` or a name this SDK does not know (onOther() takes those).
     */
    public function on(string|array $triggers, callable $handler): \Closure
    {
        $list = is_array($triggers) ? array_values($triggers) : [$triggers];
        foreach ($list as $trigger) {
            if (!is_string($trigger) || WebhookTriggers::deliveryType($trigger) === null) {
                $name = is_string($trigger) ? $trigger : get_debug_type($trigger);
                throw new \InvalidArgumentException("'{$name}' has no delivery model; register it with onOther()");
            }
        }

        $ids = [];
        foreach ($list as $trigger) {
            $id = $this->nextId++;
            $this->handlers[$trigger][$id] = $handler;
            $ids[$trigger][] = $id;
        }

        return function () use ($ids): void {
            foreach ($ids as $trigger => $own) {
                foreach ($own as $id) {
                    unset($this->handlers[$trigger][$id]);
                }
                if (($this->handlers[$trigger] ?? null) === []) {
                    unset($this->handlers[$trigger]);
                }
            }
        };
    }

    /**
     * Removes $handler from each of $triggers, every time it was registered.
     * Closures compare by identity: pass the same closure object on() got.
     *
     * @param string|string[] $triggers
     */
    public function off(string|array $triggers, callable $handler): void
    {
        foreach (is_array($triggers) ? $triggers : [$triggers] as $trigger) {
            $left = array_filter($this->handlers[$trigger] ?? [], static fn ($h) => $h !== $handler);
            if ($left === []) {
                unset($this->handlers[$trigger]);
            } else {
                $this->handlers[$trigger] = $left;
            }
        }
    }

    /**
     * Registers $handler for the deliveries no other handler takes: triggers
     * without a registration, the untyped catch-all `*`, and triggers added
     * after this SDK was built. It is called as `$handler($envelope, $trigger)`
     * with a WebhookEnvelope whose payload is the decoded JSON as it came.
     * Returns the closure that removes this registration.
     */
    public function onOther(callable $handler): \Closure
    {
        $id = $this->nextId++;
        $this->otherHandlers[$id] = $handler;

        return function () use ($id): void {
            unset($this->otherHandlers[$id]);
        };
    }

    /**
     * Handles one delivery.
     *
     * @param string $body The request body, exactly as received.
     * @param string|null $signature The `x-docspace-signature-256` header.
     * @param string|null $eventIdHeader The `x-docspace-event-id` header, if at
     *   hand: an already processed id is dropped before any work. It is not
     *   signed, so it is only ever used to drop a delivery, never to accept one.
     */
    public function handle(string $body, ?string $signature, ?string $eventIdHeader = null): WebhookHandleResult
    {
        $headerId = $eventIdHeader === null ? false : filter_var($eventIdHeader, FILTER_VALIDATE_INT);
        if ($this->store !== null && is_int($headerId) && $this->store->isProcessed($headerId)) {
            return new WebhookHandleResult(WebhookOutcome::Duplicate, 200, eventId: $headerId);
        }

        if ($this->secret !== null && !WebhookSignature::verify($body, $this->secret, $signature)) {
            return new WebhookHandleResult(WebhookOutcome::Rejected, 401, error: 'signature is missing or does not match');
        }

        try {
            [$trigger, $envelope, $delivery] = self::parse($body);
        } catch (\UnexpectedValueException $e) {
            return new WebhookHandleResult(WebhookOutcome::Invalid, 400, error: $e->getMessage(), exception: $e);
        }

        $eventId = $envelope->getEvent()?->getId();

        if ($this->store !== null && $eventId !== null && $this->store->isProcessed($eventId)) {
            return new WebhookHandleResult(WebhookOutcome::Duplicate, 200, $trigger, $eventId);
        }

        // Snapshots: arrays are values, so a handler that registers or removes
        // one does not change the list being walked.
        $typed = $delivery === null ? [] : array_values($this->handlers[$trigger] ?? []);
        $other = $typed === [] ? array_values($this->otherHandlers) : [];

        try {
            foreach ($typed as $handler) {
                $handler($delivery, $trigger);
            }
            foreach ($other as $handler) {
                $handler($envelope, $trigger);
            }
        } catch (\Throwable $e) {
            return new WebhookHandleResult(WebhookOutcome::Failed, 500, $trigger, $eventId, $e->getMessage(), $e);
        }

        if ($this->store !== null && $eventId !== null) {
            $this->store->markProcessed($eventId);
        }

        return new WebhookHandleResult(
            $typed !== [] || $other !== [] ? WebhookOutcome::Handled : WebhookOutcome::Unhandled,
            200,
            $trigger,
            $eventId);
    }

    /**
     * The trigger, the envelope with its payload as decoded, and the delivery
     * model of the trigger (null when it has none).
     *
     * @return array{0: string, 1: WebhookEnvelope, 2: object|null}
     * @throws \UnexpectedValueException The body is not a delivery.
     */
    private static function parse(string $body): array
    {
        try {
            $json = json_decode($body, false, 512, JSON_THROW_ON_ERROR);
        } catch (\JsonException $e) {
            throw new \UnexpectedValueException('body is not JSON: ' . $e->getMessage(), 0, $e);
        }

        // json_decode gives a JSON object as stdClass and a JSON array as array.
        if (!is_object($json)) {
            throw new \UnexpectedValueException('body is not a webhook envelope');
        }

        $trigger = is_object($json->event ?? null) ? ($json->event->trigger ?? null) : null;
        if (!is_string($trigger)) {
            throw new \UnexpectedValueException('envelope has no event.trigger');
        }

        $model = WebhookTriggers::deliveryType($trigger);

        // A typed trigger always carries its payload object; the deserializer
        // passes null through, which would hand the handler an empty payload.
        if ($model !== null && !is_object($json->payload ?? null)) {
            throw new \UnexpectedValueException("no payload object for '{$trigger}'");
        }

        try {
            $envelope = ObjectSerializer::deserialize($json, WebhookEnvelope::class);
            $delivery = $model === null ? null : ObjectSerializer::deserialize($json, $model);
        } catch (\Throwable $e) {
            throw new \UnexpectedValueException("unparseable '{$trigger}' delivery: " . $e->getMessage(), 0, $e);
        }

        if (!$envelope instanceof WebhookEnvelope || ($model !== null && !$delivery instanceof $model)) {
            throw new \UnexpectedValueException("unparseable '{$trigger}' delivery");
        }

        return [$trigger, $envelope, $delivery];
    }
}
