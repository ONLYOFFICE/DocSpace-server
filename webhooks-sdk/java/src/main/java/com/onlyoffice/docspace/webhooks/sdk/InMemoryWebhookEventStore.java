package com.onlyoffice.docspace.webhooks.sdk;

import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;

/** A {@link WebhookEventStore} in process memory: for samples and tests, lost on restart. */
public final class InMemoryWebhookEventStore implements WebhookEventStore {

    private final Set<Integer> ids = ConcurrentHashMap.newKeySet();

    @Override
    public boolean isProcessed(int eventId) {
        return ids.contains(eventId);
    }

    @Override
    public void markProcessed(int eventId) {
        ids.add(eventId);
    }
}
