package com.onlyoffice.docspace.webhooks.sdk;

import com.google.gson.JsonElement;

import com.onlyoffice.docspace.webhooks.sdk.model.WebhookConfigInfo;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookEventInfo;

/**
 * A delivery as an {@link OtherHandler} gets it: the envelope read, the payload
 * left as it arrived, because its trigger has no model in this build or no
 * handler took it.
 *
 * @param trigger the delivery's {@code event.trigger}
 * @param event   the event the delivery is about
 * @param webhook the subscription that sent it
 * @param payload the payload as received; null when the body has none
 */
public record RawWebhook(
        String trigger,
        WebhookEventInfo event,
        WebhookConfigInfo webhook,
        JsonElement payload) {

    /** True when the trigger is in the contract this build was generated from, the catch-all included. */
    public boolean isKnownTrigger() {
        return WebhookTriggers.isKnown(trigger);
    }
}
