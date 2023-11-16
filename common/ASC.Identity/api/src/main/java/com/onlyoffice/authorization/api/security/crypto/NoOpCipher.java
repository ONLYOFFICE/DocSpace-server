/**
 *
 */
package com.onlyoffice.authorization.api.security.crypto;

import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 *
 */
@Profile(value = {"dev", "development", "d"})
@Component
@Slf4j
public class NoOpCipher implements Cipher {
    public String encrypt(String plainMessage) {
        MDC.put("plain_message", plainMessage);
        log.info("Trying to encrypt plain message");
        MDC.clear();
        return plainMessage;
    }

    public String decrypt(String cipherMessage) {
        MDC.put("cipher_message", cipherMessage);
        log.info("Trying to decrypt cipher message");
        MDC.clear();
        return cipherMessage;
    }
}
