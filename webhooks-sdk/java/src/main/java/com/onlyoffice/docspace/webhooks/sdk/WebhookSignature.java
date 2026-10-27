package com.onlyoffice.docspace.webhooks.sdk;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.security.GeneralSecurityException;
import java.security.MessageDigest;
import java.util.HexFormat;
import java.util.Locale;

/** The {@code x-docspace-signature-256} header: {@code sha256=} + HMAC-SHA256 of the raw body. */
public final class WebhookSignature {

    /** The header DocSpace signs every delivery in. */
    public static final String HEADER_NAME = "x-docspace-signature-256";

    /** The header carrying {@code event.id}; not signed, so only ever used to drop a duplicate. */
    public static final String EVENT_ID_HEADER_NAME = "x-docspace-event-id";

    private WebhookSignature() {
    }

    /** {@code sha256=} + UPPERCASE hex, mirroring {@code WebhookSender.GetSecretHash}. */
    public static String compute(byte[] body, String secret) {
        try {
            Mac mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
            return "sha256=" + HexFormat.of().withUpperCase().formatHex(mac.doFinal(body));
        } catch (GeneralSecurityException e) {
            // HmacSHA256 is a mandatory JDK algorithm
            throw new IllegalStateException(e);
        }
    }

    /**
     * Constant-time, case-insensitive check over the RAW body. DocSpace emits
     * uppercase hex where GitHub emits lowercase, so the comparison folds case.
     * A missing or malformed header is simply not a match.
     */
    public static boolean verify(byte[] body, String secret, String signature) {
        if (body == null || secret == null || signature == null || signature.isBlank()) {
            return false;
        }
        byte[] expected = compute(body, secret).toLowerCase(Locale.ROOT).getBytes(StandardCharsets.UTF_8);
        byte[] received = signature.trim().toLowerCase(Locale.ROOT).getBytes(StandardCharsets.UTF_8);
        return MessageDigest.isEqual(expected, received);
    }
}
