package com.onlyoffice.docspace.webhooks.sdk;

/**
 * A handler of the deliveries no other handler takes: triggers without a
 * registration, the untyped catch-all {@code *}, and triggers added after this
 * SDK was built. Throwing makes the delivery a failed attempt.
 */
@FunctionalInterface
public interface OtherHandler {
    void handle(RawWebhook webhook) throws Exception;
}
