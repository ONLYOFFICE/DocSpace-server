package com.onlyoffice.docspace.webhooks.sdk;

/**
 * Why a body is not a delivery: not a JSON object, no {@code event.trigger}, or
 * a typed trigger without its payload object or with one its model rejects. The
 * router reports it as the cause of an {@code invalid} result.
 */
public final class WebhookParseException extends Exception {

    public WebhookParseException(String message, Throwable cause) {
        super(message, cause);
    }
}
