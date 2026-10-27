package com.onlyoffice.docspace.webhooks.sdk;

/**
 * The result of one delivery: what happened and the HTTP status to answer
 * DocSpace with. A 2xx acknowledges the delivery; anything else is a failed
 * attempt, which DocSpace retries on its schedule.
 *
 * @param outcome    what the router did
 * @param statusCode the HTTP status to answer with
 * @param trigger    the delivery's {@code event.trigger}, when the body was read that far
 * @param eventId    the delivery's {@code event.id} (or the header's, for a duplicate dropped by it)
 * @param error      why the delivery was rejected, invalid or failed
 * @param cause      the exception behind {@code error}, if any
 */
public record WebhookHandleResult(
        WebhookOutcome outcome,
        int statusCode,
        String trigger,
        Integer eventId,
        String error,
        Throwable cause) {

    /** True for a 2xx: the delivery is acknowledged and DocSpace does not retry it. */
    public boolean isAcknowledged() {
        return statusCode >= 200 && statusCode < 300;
    }
}
