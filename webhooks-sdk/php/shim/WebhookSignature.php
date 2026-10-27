<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/** The `x-docspace-signature-256` header: `sha256=` + hex HMAC-SHA256 of the raw body. */
final class WebhookSignature
{
    public const HEADER = 'x-docspace-signature-256';

    /** `sha256=` + UPPERCASE hex, as DocSpace sends it. */
    public static function compute(string $body, string $secret): string
    {
        return 'sha256=' . strtoupper(hash_hmac('sha256', $body, $secret));
    }

    /**
     * Constant-time, case-insensitive check over the RAW body. DocSpace emits
     * uppercase hex where GitHub emits lowercase, so the comparison folds case.
     * A missing or malformed header is false, never an exception.
     */
    public static function verify(string $body, string $secret, ?string $signature): bool
    {
        if ($signature === null || $signature === '') {
            return false;
        }
        return hash_equals(
            strtolower(self::compute($body, $secret)),
            strtolower(trim($signature))
        );
    }
}
