package com.asc.authorization.web.security.crypto.cipher;

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
