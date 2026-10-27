<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/**
 * Remembers which events were processed. DocSpace delivers at least once: an
 * automatic or manual retry sends the same `event.id` again, and the signature
 * does not prove when a request was sent, so the id is the duplicate and replay
 * defence. A real receiver persists the ids for as long as an event can still
 * arrive -- the automatic retries span about 18 hours on the default schedule,
 * and a manual retry can come at any time later.
 */
interface WebhookEventStore
{
    /** True when the event was processed before. */
    public function isProcessed(int $eventId): bool;

    /** Records the event as processed; called only after every handler completed. */
    public function markProcessed(int $eventId): void;
}
