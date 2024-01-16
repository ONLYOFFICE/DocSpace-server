/**
 *
 */
package com.asc.authorization.api.unit.crypto;

import com.asc.authorization.api.web.security.crypto.NoOpCipher;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import static org.junit.Assert.assertEquals;

/**
 *
 */
@ActiveProfiles("test")
public class NoOpCipherTest {
    private NoOpCipher cipher = new NoOpCipher();

    @Test
    void shouldEncrypt() {
        String plainText = "mock";
        var encrypted = cipher.encrypt(plainText);
        assertEquals(plainText, encrypted);
    }

    @Test
    void shouldDecrypt() {
        String plainText = "mock";
        var decrypted = cipher.decrypt(cipher.encrypt(plainText));
        assertEquals(plainText, decrypted);
    }
}
