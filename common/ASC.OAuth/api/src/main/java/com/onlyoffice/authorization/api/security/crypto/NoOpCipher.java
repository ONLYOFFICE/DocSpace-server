/**
 *
 */
package com.onlyoffice.authorization.api.security.crypto;

import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 *
 */
@Profile(value = {"dev", "development", "test", "testing", "d", "t"})
@Component
public class NoOpCipher implements Cipher {
    public String encrypt(String plainMessage) {
        return plainMessage;
    }

    public String decrypt(String cipherMessage) {
        return cipherMessage;
    }
}
