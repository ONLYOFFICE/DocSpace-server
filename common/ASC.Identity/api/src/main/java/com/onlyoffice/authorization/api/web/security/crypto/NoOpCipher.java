/**
 *
 */
package com.onlyoffice.authorization.api.web.security.crypto;

import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 *
 */
@Slf4j
@Component
@Profile(value = {"dev", "development", "d"})
public class NoOpCipher implements Cipher {
    /**
     *
     * @param plainMessage
     * @return
     */
    public String encrypt(String plainMessage) {
        MDC.put("plainMessage", plainMessage);
        log.debug("Trying to encrypt plain message");
        MDC.clear();

        return plainMessage;
    }

    /**
     *
     * @param cipherMessage
     * @return
     */
    public String decrypt(String cipherMessage) {
        MDC.put("cipherMessage", cipherMessage);
        log.debug("Trying to decrypt cipher message");
        MDC.clear();

        return cipherMessage;
    }
}
