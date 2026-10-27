<?php
// Hand-written; copied into generated/lib/ by the generate-webhook-sdk post-step.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/** What WebhookRouter::handle() did with a delivery. */
enum WebhookOutcome: string
{
    /** At least one handler ran, and all of them completed. */
    case Handled = 'handled';

    /** The event was processed before; nothing ran. */
    case Duplicate = 'duplicate';

    /** No handler is registered for the trigger; the delivery is acknowledged all the same. */
    case Unhandled = 'unhandled';

    /** The signature is missing or does not match. */
    case Rejected = 'rejected';

    /** The body is not a delivery, or a typed trigger arrived without its payload. */
    case Invalid = 'invalid';

    /** A handler threw; the remaining ones did not run. */
    case Failed = 'failed';
}
