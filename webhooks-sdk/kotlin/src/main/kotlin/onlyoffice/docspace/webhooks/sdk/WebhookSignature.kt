package onlyoffice.docspace.webhooks.sdk

import java.security.MessageDigest
import java.util.HexFormat
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

/** The `x-docspace-signature-256` check over the raw request body. */
object WebhookSignature {
    const val HEADER = "x-docspace-signature-256"

    /** The unsigned event id header; see [WebhookRouter.handle]. */
    const val EVENT_ID_HEADER = "x-docspace-event-id"

    /** `sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash. */
    fun compute(body: ByteArray, secret: String): String {
        val mac = Mac.getInstance("HmacSHA256")
        mac.init(SecretKeySpec(secret.toByteArray(Charsets.UTF_8), "HmacSHA256"))
        return "sha256=" + HexFormat.of().withUpperCase().formatHex(mac.doFinal(body))
    }

    /**
     * Constant-time, case-insensitive check over the RAW body. DocSpace emits
     * uppercase hex where GitHub emits lowercase, so the comparison folds case.
     * A missing or malformed header is false, never an exception.
     */
    fun verify(body: ByteArray, secret: String, signature: String?): Boolean {
        if (signature.isNullOrBlank()) return false
        return MessageDigest.isEqual(
            compute(body, secret).lowercase().toByteArray(Charsets.UTF_8),
            signature.trim().lowercase().toByteArray(Charsets.UTF_8),
        )
    }
}
