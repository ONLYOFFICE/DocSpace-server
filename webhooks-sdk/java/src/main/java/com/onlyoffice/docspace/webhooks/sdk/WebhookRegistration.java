package com.onlyoffice.docspace.webhooks.sdk;

/** One handler registration; closing it removes exactly that handler. Closing twice does nothing. */
public interface WebhookRegistration extends AutoCloseable {

    @Override
    void close();
}
