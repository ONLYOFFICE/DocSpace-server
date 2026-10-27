package com.onlyoffice.docspace.webhooks.sdk;

/**
 * A handler of one trigger, called with the body read into the trigger's
 * delivery model ({@code FileDelivery} for {@code file.created}, ...). Throwing
 * makes the delivery a failed attempt, which DocSpace retries.
 */
@FunctionalInterface
public interface DeliveryHandler<T> {
    void handle(T delivery) throws Exception;
}
