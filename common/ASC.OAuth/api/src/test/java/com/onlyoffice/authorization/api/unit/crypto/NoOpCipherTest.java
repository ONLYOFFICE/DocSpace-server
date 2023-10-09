package com.onlyoffice.authorization.api.unit.crypto;

import com.onlyoffice.authorization.api.crypto.NoOpCipher;
import org.junit.jupiter.api.Test;
import org.springframework.test.context.ActiveProfiles;

import static org.junit.Assert.*;

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
