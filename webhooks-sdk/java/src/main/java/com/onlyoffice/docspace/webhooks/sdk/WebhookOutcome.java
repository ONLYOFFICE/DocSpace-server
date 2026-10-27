package com.onlyoffice.docspace.webhooks.sdk;

/** What {@link AbstractWebhookRouter#handle(byte[], String, String)} did with a delivery. */
public enum WebhookOutcome {
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
    FAILED
}
