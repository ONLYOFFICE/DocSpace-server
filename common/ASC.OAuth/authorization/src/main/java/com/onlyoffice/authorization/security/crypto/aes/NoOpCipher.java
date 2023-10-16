package com.onlyoffice.authorization.security.crypto.aes;

import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

@Profile(value = {"dev", "development", "d"})
@Component
public class NoOpCipher implements Cipher {
    public String encrypt(String plainMessage) {
        return plainMessage;
    }

    public String decrypt(String cipherMessage) {
        return cipherMessage;
    }
}
