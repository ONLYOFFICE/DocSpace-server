package com.onlyoffice.docspace.webhooks.sdk;

/**
 * Remembers which events were processed. DocSpace delivers at least once: an
 * automatic or manual retry sends the same {@code event.id} again, and the
 * signature does not prove when a request was sent, so the id is the duplicate
 * and replay defence. A real receiver persists the ids for as long as an event
 * can still arrive -- the automatic retries span about 18 hours on the default
 * schedule, and a manual retry can come at any time later.
 */
public interface WebhookEventStore {

    /** True when the event was processed before. */
    boolean isProcessed(int eventId);

    /** Records the event as processed; called only after every handler completed. */
    void markProcessed(int eventId);
}
