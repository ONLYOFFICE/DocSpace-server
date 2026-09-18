package com.onlyoffice.docspace.webhooks.samples;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import com.onlyoffice.docspace.webhooks.sdk.JSON;
import com.onlyoffice.docspace.webhooks.sdk.model.FileEntryPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupPayload;
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

        if (trigger.startsWith("user.")) {
            UserPayload u = UserPayload.fromJson(payloadJson);
            System.out.printf("  user: %s <%s>%n", u.getUserName(), u.getEmail());
        } else if (trigger.startsWith("group.")) {
            GroupPayload g = GroupPayload.fromJson(payloadJson);
            System.out.printf("  group: %s%n", g.getName());
        } else {
            // file.*, folder.*, room.*, agent.* and form.* all arrive as the
            // FileEntry<T> base; fileEntryType is the only discriminator.
            FileEntryPayload e = FileEntryPayload.fromJson(payloadJson);
            String kind = Integer.valueOf(2).equals(e.getFileEntryType()) ? "file" : "folder";
            System.out.printf("  %s: %s  id=%s  parent=%s%n",
                    kind, e.getTitle(), e.getId(), e.getParentId());
        }

        System.out.println();
        String pretty = JSON.getGson().newBuilder().setPrettyPrinting().create()
                .toJson(envelope.get("payload"));
        System.out.println("  " + pretty.replace("\n", "\n  "));
    }
}
