<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/**
 * A WebhookEventStore in process memory: for samples and tests. It is lost when
 * the process ends, which under PHP-FPM is after every request -- a real
 * receiver keeps the ids in a database or a cache.
 */
final class InMemoryWebhookEventStore implements WebhookEventStore
{
    /** @var array<int, true> */
    private array $ids = [];

    public function isProcessed(int $eventId): bool
    {
        return isset($this->ids[$eventId]);
    }

    public function markProcessed(int $eventId): void
    {
        $this->ids[$eventId] = true;
    }
}
