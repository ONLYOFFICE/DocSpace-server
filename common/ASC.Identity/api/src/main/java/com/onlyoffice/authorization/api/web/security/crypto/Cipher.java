/**
 *
 */
package com.onlyoffice.authorization.api.web.security.crypto;

/**
 *
 */
public interface Cipher {
    String encrypt(String plainMessage) throws Exception;
    String decrypt(String cipherMessage) throws Exception;
}
