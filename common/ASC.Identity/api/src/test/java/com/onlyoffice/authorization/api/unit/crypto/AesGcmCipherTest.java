/**
 *
 */
package com.onlyoffice.authorization.api.unit.crypto;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.web.security.crypto.AesGcmCipher;
import lombok.SneakyThrows;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotEquals;

/**
 *
 */
@ActiveProfiles("test")
public class AesGcmCipherTest {
    private AesGcmCipher cipher = new AesGcmCipher(new ApplicationConfiguration());

    @Test
    @SneakyThrows
    void shouldEncrypt() {
        String plainText = "mock";
        var encrypted = cipher.encrypt(plainText);
        assertNotEquals(plainText, encrypted);
    }

    @Test
    @SneakyThrows
    void shouldDecrypt() {
        String plainText = "mock";
        var decrypted = cipher.decrypt(cipher.encrypt(plainText));
        assertEquals(plainText, decrypted);
    }
}
