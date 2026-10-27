package com.onlyoffice.docspace.webhooks.sdk;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import com.onlyoffice.docspace.webhooks.sdk.model.WebhookConfigInfo;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookEventInfo;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.function.Predicate;

/**
 * Dispatches verified deliveries to handlers registered per trigger
 * ({@code onFileCreated}, ... on the generated {@link WebhookRouter}, or
 * {@link #on(String, Class, DeliveryHandler)}), or for anything else
 * ({@link #onOther(OtherHandler)}). Any number of handlers can be registered for
 * one trigger; each registration returns a {@link WebhookRegistration} that
 * removes it.
 *
 * <p>{@link #handle(byte[], String, String)} checks the signature, drops a
 * duplicate, reads the body into the delivery model of its trigger and calls
 * every handler of that trigger in registration order. Answer DocSpace with the
 * result's {@code statusCode} only after it returns: a handler that throws makes
 * it a failed attempt, and DocSpace sends the delivery again, so handlers have
 * to be idempotent.
 *
 * <p>Registering and removing handlers is safe while deliveries are handled:
 * the registrations are replaced, never changed in place, and every delivery
 * works on one snapshot of them.
 */
public abstract class AbstractWebhookRouter {

    private final String secret;
    private final WebhookEventStore store;
    private final Object sync = new Object();

    private volatile Map<String, List<Registration>> byTrigger = Map.of();
    private volatile List<Registration> other = List.of();

    /**
     * @param secret the secret key of the subscription; null makes an
     *               unsigned-replay router, which does not check signatures
     *               (development only, never behind a reachable endpoint)
     * @param store  where processed event ids are kept; without one (null),
     *               duplicates are not detected
     */
    protected AbstractWebhookRouter(String secret, WebhookEventStore store) {
        this.secret = secret;
        this.store = store;
    }

    /** Returns {@code secret}, or throws when it is null or empty. */
    protected static String requireSecret(String secret) {
        if (secret == null || secret.isEmpty()) {
            throw new IllegalArgumentException("a secret is required; use forUnsignedReplay to replay unsigned bodies");
        }
        return secret;
    }

    /**
     * Registers {@code handler} for {@code trigger}, which has to be delivered as
     * {@code type}. The generated {@code on<Trigger>} methods call this.
     *
     * @throws IllegalArgumentException the trigger has no delivery model, or it is not {@code type}
     */
    public <T> WebhookRegistration on(String trigger, Class<T> type, DeliveryHandler<T> handler) {
        Objects.requireNonNull(type, "type");
        Objects.requireNonNull(handler, "handler");

        Class<?> expected = WebhookTriggers.deliveryType(trigger);
        if (expected != type) {
            throw new IllegalArgumentException("'" + trigger + "' is delivered as "
                    + (expected == null ? "an untyped body" : expected.getSimpleName())
                    + ", not " + type.getSimpleName());
        }

        Registration registration = new Registration(handler, (delivery, hook) -> handler.handle(type.cast(delivery)));

        synchronized (sync) {
            Map<String, List<Registration>> copy = new HashMap<>(byTrigger);
            copy.put(trigger, with(copy.get(trigger), registration));
            byTrigger = Collections.unmodifiableMap(copy);
        }

        return unsubscriber(() -> {
            synchronized (sync) {
                byTrigger = without(byTrigger, trigger, r -> r == registration);
            }
        });
    }

    /** Removes every registration of {@code handler} for {@code trigger}. */
    public void off(String trigger, DeliveryHandler<?> handler) {
        synchronized (sync) {
            byTrigger = without(byTrigger, trigger, r -> r.handler == handler);
        }
    }

    /**
     * Registers {@code handler} for the deliveries no other handler takes:
     * triggers without a registration, the untyped catch-all {@code *}, and
     * triggers added after this SDK was built. It gets the envelope with the
     * payload as it arrived.
     */
    public WebhookRegistration onOther(OtherHandler handler) {
        Objects.requireNonNull(handler, "handler");

        Registration registration = new Registration(handler, (delivery, hook) -> handler.handle(hook));

        synchronized (sync) {
            other = with(other, registration);
        }

        return unsubscriber(() -> {
            synchronized (sync) {
                other = other.stream().filter(r -> r != registration).toList();
            }
        });
    }

    /** Handles one delivery whose body arrived as text; see {@link #handle(byte[], String, String)}. */
    public WebhookHandleResult handle(String body, String signature, String eventIdHeader) {
        Objects.requireNonNull(body, "body");
        return handle(body.getBytes(StandardCharsets.UTF_8), signature, eventIdHeader);
    }

    /**
     * Handles one delivery; see the description of {@link AbstractWebhookRouter}.
     *
     * @param body          the request body, exactly as received
     * @param signature     the {@code x-docspace-signature-256} header
     * @param eventIdHeader the {@code x-docspace-event-id} header, or null: an
     *                      already processed id is dropped before any work. The
     *                      header is not signed, so it is only ever used to drop a
     *                      delivery, never to accept one.
     */
    public WebhookHandleResult handle(byte[] body, String signature, String eventIdHeader) {
        Objects.requireNonNull(body, "body");

        Integer headerId = parseId(eventIdHeader);
        if (store != null && headerId != null && store.isProcessed(headerId)) {
            return new WebhookHandleResult(WebhookOutcome.DUPLICATE, 200, null, headerId, null, null);
        }

        if (secret != null && !WebhookSignature.verify(body, secret, signature)) {
            return new WebhookHandleResult(WebhookOutcome.REJECTED, 401, null, null,
                    "signature is missing or does not match", null);
        }

        RawWebhook hook;
        Object delivery;
        try {
            JsonObject envelope = readEnvelope(body);
            hook = readHook(envelope);
            delivery = readDelivery(envelope, hook.trigger());
        } catch (WebhookParseException e) {
            return new WebhookHandleResult(WebhookOutcome.INVALID, 400, null, null, e.getMessage(), e);
        }

        String trigger = hook.trigger();
        Integer eventId = hook.event() == null ? null : hook.event().getId();

        if (store != null && eventId != null && store.isProcessed(eventId)) {
            return new WebhookHandleResult(WebhookOutcome.DUPLICATE, 200, trigger, eventId, null, null);
        }

        List<Registration> handlers = getHandlers(trigger, delivery);

        for (Registration handler : handlers) {
            try {
                handler.invoker.invoke(delivery, hook);
            } catch (Exception e) {
                if (e instanceof InterruptedException) {
                    Thread.currentThread().interrupt();
                }
                return new WebhookHandleResult(WebhookOutcome.FAILED, 500, trigger, eventId,
                        e.getMessage() != null ? e.getMessage() : e.toString(), e);
            }
        }

        if (store != null && eventId != null) {
            store.markProcessed(eventId);
        }

        return new WebhookHandleResult(handlers.isEmpty() ? WebhookOutcome.UNHANDLED : WebhookOutcome.HANDLED,
                200, trigger, eventId, null, null);
    }

    private List<Registration> getHandlers(String trigger, Object delivery) {
        // one consistent snapshot: registrations are replaced, never changed in place
        Map<String, List<Registration>> typed = byTrigger;
        List<Registration> rest = other;

        if (delivery == null) {
            return rest;
        }

        List<Registration> own = typed.getOrDefault(trigger, List.of());
        return own.isEmpty() ? rest : own;
    }

    private static JsonObject readEnvelope(byte[] body) throws WebhookParseException {
        JsonElement root;
        try {
            root = JsonParser.parseString(new String(body, StandardCharsets.UTF_8));
        } catch (RuntimeException e) {
            throw new WebhookParseException("the body is not JSON: " + e.getMessage(), e);
        }

        if (!root.isJsonObject()) {
            throw new WebhookParseException("the body is not a JSON object", null);
        }

        JsonObject envelope = root.getAsJsonObject();
        JsonElement event = envelope.get("event");
        JsonElement trigger = event != null && event.isJsonObject() ? event.getAsJsonObject().get("trigger") : null;

        if (trigger == null || !trigger.isJsonPrimitive() || !trigger.getAsJsonPrimitive().isString()) {
            throw new WebhookParseException("the envelope has no event.trigger", null);
        }

        return envelope;
    }

    private static RawWebhook readHook(JsonObject envelope) throws WebhookParseException {
        String trigger = envelope.getAsJsonObject("event").get("trigger").getAsString();
        try {
            WebhookEventInfo event = JSON.getGson().fromJson(envelope.get("event"), WebhookEventInfo.class);
            WebhookConfigInfo webhook = JSON.getGson().fromJson(envelope.get("webhook"), WebhookConfigInfo.class);
            return new RawWebhook(trigger, event, webhook, envelope.get("payload"));
        } catch (RuntimeException e) {
            throw new WebhookParseException("unparseable '" + trigger + "' envelope: " + e.getMessage(), e);
        }
    }

    /** The body read whole into the trigger's delivery model; null when the trigger has none. */
    private static Object readDelivery(JsonObject envelope, String trigger) throws WebhookParseException {
        Class<?> model = WebhookTriggers.deliveryType(trigger);
        if (model == null) {
            return null;
        }

        // A typed trigger always carries its payload object; Gson would read a
        // missing or null one as a delivery without payload rather than failing.
        JsonElement payload = envelope.get("payload");
        if (payload == null || !payload.isJsonObject()) {
            throw new WebhookParseException("no payload object for '" + trigger + "'", null);
        }

        Object delivery;
        try {
            // the model's own type adapter validates the JSON against the contract
            delivery = JSON.getGson().fromJson(envelope, model);
        } catch (RuntimeException e) {
            throw new WebhookParseException("unparseable '" + trigger + "' delivery: " + e.getMessage(), e);
        }

        if (delivery == null) {
            throw new WebhookParseException("no " + model.getSimpleName() + " for '" + trigger + "'", null);
        }
        return delivery;
    }

    private static Integer parseId(String header) {
        if (header == null) {
            return null;
        }
        try {
            return Integer.parseInt(header.trim());
        } catch (NumberFormatException e) {
            return null;
        }
    }

    private static List<Registration> with(List<Registration> source, Registration registration) {
        List<Registration> copy = source == null ? new ArrayList<>() : new ArrayList<>(source);
        copy.add(registration);
        return Collections.unmodifiableList(copy);
    }

    private static Map<String, List<Registration>> without(
            Map<String, List<Registration>> source, String trigger, Predicate<Registration> match) {
        List<Registration> current = source.get(trigger);
        if (current == null) {
            return source;
        }

        List<Registration> left = current.stream().filter(match.negate()).toList();
        Map<String, List<Registration>> copy = new HashMap<>(source);
        if (left.isEmpty()) {
            copy.remove(trigger);
        } else {
            copy.put(trigger, left);
        }
        return Collections.unmodifiableMap(copy);
    }

    private static WebhookRegistration unsubscriber(Runnable unsubscribe) {
        AtomicBoolean closed = new AtomicBoolean();
        return () -> {
            if (closed.compareAndSet(false, true)) {
                unsubscribe.run();
            }
        };
    }

    @FunctionalInterface
    private interface Invoker {
        void invoke(Object delivery, RawWebhook hook) throws Exception;
    }

    private static final class Registration {
        final Object handler;
        final Invoker invoker;

        Registration(Object handler, Invoker invoker) {
            this.handler = handler;
            this.invoker = invoker;
        }
    }
}
