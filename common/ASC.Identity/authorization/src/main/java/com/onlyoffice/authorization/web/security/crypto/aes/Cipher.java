package com.onlyoffice.authorization.web.security.crypto.aes;

/**
 *
 */
public interface Cipher {
    /**
     *
     * @param plainMessage
     * @return
     * @throws Exception
     */
    String encrypt(String plainMessage) throws Exception;

    /**
     *
     * @param cipherMessage
     * @return
     * @throws Exception
     */
    String decrypt(String cipherMessage) throws Exception;
}
