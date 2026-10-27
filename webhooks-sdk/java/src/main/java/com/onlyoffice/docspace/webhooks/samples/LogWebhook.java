package com.onlyoffice.docspace.webhooks.samples;

import com.google.gson.Gson;
import com.google.gson.JsonElement;

import com.onlyoffice.docspace.webhooks.sdk.InMemoryWebhookEventStore;
import com.onlyoffice.docspace.webhooks.sdk.JSON;
import com.onlyoffice.docspace.webhooks.sdk.WebhookHandleResult;
import com.onlyoffice.docspace.webhooks.sdk.WebhookRouter;
import com.onlyoffice.docspace.webhooks.sdk.WebhookTriggers;
import com.onlyoffice.docspace.webhooks.sdk.model.FileDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.FilePayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FolderDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.FolderPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FormSubmitDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.FormSubmitPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.RoomDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.RoomPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.UserDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.UserPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookConfigInfo;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookEventInfo;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;

/**
 * Logs a DocSpace webhook delivery to the console, through the SDK's router.
 *
 * <pre>
 *   mvn -q compile exec:java                                   # shared fixture
 *   mvn -q compile exec:java -Dexec.args="&lt;body&gt; &lt;secret&gt; &lt;sig&gt;"
 * </pre>
 */
public final class LogWebhook {

    private static final Gson PRETTY = JSON.getGson().newBuilder().setPrettyPrinting().create();

    private LogWebhook() {
    }

    /**
     * The payload model the generated trigger table names: "untyped" for the
     * legacy catch-all, "unknown" for a trigger newer than this SDK.
     */
    static String modelName(String trigger) {
        Class<?> model = WebhookTriggers.payloadType(trigger);
        if (model != null) {
            return model.getSimpleName();
        }
        return WebhookTriggers.UNTYPED_TRIGGERS.contains(trigger) ? "untyped" : "unknown";
    }

    /**
     * The router this sample hands the delivery to: one handler per trigger with
     * a delivery model, taken from the generated table; onFileCreated, one of the
     * generated on&lt;Trigger&gt; methods, adds a second one for file.created; and
     * onOther takes what has no handler: the catch-all and triggers newer than
     * this SDK.
     */
    static WebhookRouter createRouter(String secret) {
        // In memory for the sake of the sample; a real receiver persists the ids
        // for as long as an event can still arrive (see WebhookEventStore).
        WebhookRouter router = new WebhookRouter(secret, new InMemoryWebhookEventStore());

        for (Map.Entry<String, Class<?>> entry : WebhookTriggers.DELIVERY_TYPES.entrySet()) {
            register(router, entry.getKey(), entry.getValue());
        }

        router.onFileCreated(d -> System.out.printf("  onFileCreated: \"%s\" in folder %s%n",
                d.getPayload().getTitle(), d.getPayload().getParentId()));

        router.onOther(hook -> {
            String known = hook.isKnownTrigger() ? "" : "  (trigger unknown to this build)";
            System.out.println(hook.trigger() + known);
            System.out.println("  signature ok");
            System.out.printf("  model: %s%n", modelName(hook.trigger()));
            System.out.println("  delivery: none");
            printEnvelope(hook.event(), hook.webhook());
            System.out.println("  no payload model for this trigger; raw payload below");
            printPayload(hook.payload());
        });

        return router;
    }

    private static <T> void register(WebhookRouter router, String trigger, Class<T> type) {
        router.on(trigger, type, delivery -> print(trigger, delivery));
    }

    private static void print(String trigger, Object delivery) {
        WebhookEventInfo event;
        WebhookConfigInfo config;
        Object payload;

        // The *Delivery models are unrelated generated classes, one per payload.
        if (delivery instanceof FileDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else if (delivery instanceof FolderDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else if (delivery instanceof RoomDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else if (delivery instanceof UserDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else if (delivery instanceof GroupDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else if (delivery instanceof FormSubmitDelivery d) {
            event = d.getEvent(); config = d.getWebhook(); payload = d.getPayload();
        } else {
            throw new IllegalStateException("no printer for " + delivery.getClass().getSimpleName());
        }

        System.out.println(trigger);
        System.out.println("  signature ok");
        System.out.printf("  model: %s%n", modelName(trigger));
        System.out.printf("  delivery: %s%n", delivery.getClass().getSimpleName());
        printEnvelope(event, config);
        System.out.println("  " + summarize(payload));
        printPayload(JSON.getGson().toJsonTree(payload));
    }

    private static void printEnvelope(WebhookEventInfo event, WebhookConfigInfo config) {
        if (event != null) {
            System.out.printf("  event #%d  at %s  by %s%n", event.getId(), event.getCreateOn(), event.getCreateBy());
        }
        if (config != null) {
            System.out.printf("  subscription #%d \"%s\"%n", config.getId(), config.getName());
        }
    }

    /** A typed line per payload kind, to show the models actually being used. */
    private static String summarize(Object payload) {
        if (payload instanceof UserPayload u) {
            return String.format("user: %s <%s>", u.getUserName(), u.getEmail());
        } else if (payload instanceof GroupPayload g) {
            return String.format("group: %s", g.getName());
        } else if (payload instanceof FormSubmitPayload f) {
            // The one wrapper payload: the original form and the submitted copy.
            return String.format("form: %s  submitted as %s",
                    f.getOriginalForm() == null ? null : f.getOriginalForm().getTitle(),
                    f.getSubmittedForm() == null ? null : f.getSubmittedForm().getTitle());
        } else if (payload instanceof FolderPayload d) {
            return String.format("folder: %s  id=%s  parent=%s", d.getTitle(), d.getId(), d.getParentId());
        } else if (payload instanceof RoomPayload r) {
            return String.format("room: %s  id=%s  parent=%s", r.getTitle(), r.getId(), r.getParentId());
        } else if (payload instanceof FilePayload e) {
            return String.format("file: %s  id=%s  parent=%s", e.getTitle(), e.getId(), e.getParentId());
        }
        return "payload: " + (payload == null ? "none" : payload.getClass().getSimpleName());
    }

    private static void printPayload(JsonElement payload) {
        System.out.println();
        System.out.println("  " + PRETTY.toJson(payload).replace("\n", "\n  "));
    }

    private static String read(Path p) throws IOException {
        return Files.readString(p).trim();
    }

    public static void main(String[] args) throws Exception {
        Path fixtures = Path.of("..", "samples");

        Path bodyPath = args.length > 0 ? Path.of(args[0]) : fixtures.resolve("file.created.json");
        String secret = args.length > 1 ? args[1] : read(fixtures.resolve("SECRET"));
        String signature = args.length > 2 ? args[2] : read(fixtures.resolve("file.created.sig"));

        // Bytes, not a re-encoded object: the signature covers what arrived.
        byte[] body = Files.readAllBytes(bodyPath);

        // A listener would answer DocSpace with result.statusCode(), and only
        // after handle returned: a 2xx acknowledges, anything else is retried.
        WebhookHandleResult result = createRouter(secret).handle(body, signature, null);

        switch (result.outcome()) {
            case HANDLED, UNHANDLED -> {
            }
            case DUPLICATE -> System.out.printf("duplicate of event #%d, already processed; skipped%n",
                    result.eventId());
            case REJECTED -> {
                System.err.println("REJECTED: " + result.error());
                System.exit(1);
            }
            case INVALID -> {
                System.err.println("unparseable body: " + result.error());
                System.exit(1);
            }
            case FAILED -> {
                System.err.println("a handler failed: " + result.error());
                System.exit(1);
            }
        }
    }
}
