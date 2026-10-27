<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/**
 * The result of one delivery: what happened and the HTTP status to answer
 * DocSpace with. A 2xx acknowledges the delivery; anything else is a failed
 * attempt, which DocSpace retries on its schedule.
 */
final class WebhookHandleResult
{
    public function __construct(
        public readonly WebhookOutcome $outcome,
        public readonly int $statusCode,
        public readonly ?string $trigger = null,
        public readonly ?int $eventId = null,
        public readonly ?string $error = null,
        public readonly ?\Throwable $exception = null,
    ) {
    }

    /** True when the status acknowledges the delivery (2xx). */
    public function isSuccess(): bool
    {
        return $this->statusCode >= 200 && $this->statusCode < 300;
    }
}
