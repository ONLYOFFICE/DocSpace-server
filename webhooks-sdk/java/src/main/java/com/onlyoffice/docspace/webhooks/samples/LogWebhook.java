package com.onlyoffice.docspace.webhooks.samples;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import com.onlyoffice.docspace.webhooks.sdk.JSON;
import com.onlyoffice.docspace.webhooks.sdk.WebhookTriggers;
import com.onlyoffice.docspace.webhooks.sdk.model.FilePayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FolderPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FormSubmitPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.RoomPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.UserPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookConfigInfo;
import com.onlyoffice.docspace.webhooks.sdk.model.WebhookEventInfo;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.HexFormat;

/**
 * Logs a DocSpace webhook delivery to the console.
 *
 * <pre>
 *   mvn -q compile exec:java                                   # shared fixture
 *   mvn -q compile exec:java -Dexec.args="&lt;body&gt; &lt;secret&gt; &lt;sig&gt;"
 * </pre>
 *
 * Java has no src/ runtime yet, so signature checking lives here.
 */
public final class LogWebhook {

    private static final String SIGNATURE_HEADER = "x-docspace-signature-256";

    private LogWebhook() {
    }

    /** {@code sha256=} + UPPERCASE hex, mirroring {@code WebhookSender.GetSecretHash}. */
    static String computeSignature(byte[] body, String secret) throws Exception {
        Mac mac = Mac.getInstance("HmacSHA256");
        mac.init(new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
        return "sha256=" + HexFormat.of().withUpperCase().formatHex(mac.doFinal(body));
    }

    /**
     * Constant-time, case-insensitive check over the RAW body. DocSpace emits
     * uppercase hex where GitHub emits lowercase, so the comparison folds case.
     */
    static boolean verifySignature(byte[] body, String secret, String signature) throws Exception {
        if (signature == null || signature.isBlank()) {
            return false;
        }
        byte[] expected = computeSignature(body, secret)
                .toLowerCase().getBytes(StandardCharsets.UTF_8);
        byte[] received = signature.trim()
                .toLowerCase().getBytes(StandardCharsets.UTF_8);
        return MessageDigest.isEqual(expected, received);
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

        if (!verifySignature(body, secret, signature)) {
            System.err.println("REJECTED: signature does not match");
            System.exit(1);
        }

        String json = new String(body, StandardCharsets.UTF_8);
        JsonObject envelope = JsonParser.parseString(json).getAsJsonObject();

        if (!envelope.has("event") || !envelope.getAsJsonObject("event").has("trigger")) {
            System.err.println("envelope has no event.trigger");
            System.exit(1);
        }

        String trigger = envelope.getAsJsonObject("event").get("trigger").getAsString();
        String payloadJson = envelope.get("payload").toString();

        WebhookEventInfo event =
                WebhookEventInfo.fromJson(envelope.get("event").toString());
        WebhookConfigInfo config =
                WebhookConfigInfo.fromJson(envelope.get("webhook").toString());

        System.out.println(trigger);
        System.out.println("  signature ok");
        System.out.printf("  event #%d  at %s  by %s%n",
                event.getId(), event.getCreateOn(), event.getCreateBy());
        System.out.printf("  subscription #%d \"%s\"%n", config.getId(), config.getName());

        Class<?> model = WebhookTriggers.payloadType(trigger);
        Object payload = null;
        if (model != null) {
            // The model's own type adapter validates the JSON against the
            // contract; a parse error ends the run with a non-zero exit.
            try {
                payload = JSON.getGson().fromJson(payloadJson, model);
            } catch (RuntimeException e) {
                System.err.printf("unparseable %s payload: %s%n", trigger, e.getMessage());
                System.exit(1);
            }
        }

        System.out.printf("  model: %s%n", modelName(trigger));

        if (payload instanceof UserPayload u) {
            System.out.printf("  user: %s <%s>%n", u.getUserName(), u.getEmail());
        } else if (payload instanceof GroupPayload g) {
            System.out.printf("  group: %s%n", g.getName());
        } else if (payload instanceof FormSubmitPayload f) {
            // The one wrapper payload: the original form and the submitted copy.
            System.out.printf("  form: %s  submitted as %s%n",
                    f.getOriginalForm() == null ? null : f.getOriginalForm().getTitle(),
                    f.getSubmittedForm() == null ? null : f.getSubmittedForm().getTitle());
        } else if (payload instanceof FolderPayload d) {
            System.out.printf("  folder: %s  id=%s  parent=%s%n",
                    d.getTitle(), d.getId(), d.getParentId());
        } else if (payload instanceof RoomPayload r) {
            System.out.printf("  room: %s  id=%s  parent=%s%n",
                    r.getTitle(), r.getId(), r.getParentId());
        } else if (payload instanceof FilePayload e) {
            System.out.printf("  file: %s  id=%s  parent=%s%n",
                    e.getTitle(), e.getId(), e.getParentId());
        } else {
            // The catch-all, or a trigger added on the server after this SDK was
            // generated: acknowledge it and keep the raw payload rather than
            // parsing it with some other model.
            System.out.println("  no payload model for this trigger; raw payload below");
        }

        System.out.println();
        String pretty = JSON.getGson().newBuilder().setPrettyPrinting().create()
                .toJson(envelope.get("payload"));
        System.out.println("  " + pretty.replace("\n", "\n  "));
    }
}
