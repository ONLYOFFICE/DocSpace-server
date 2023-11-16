/**
 *
 */
package com.onlyoffice.authorization.api.security.crypto;

/**
 *
 */
public interface Cipher {
    String encrypt(String plainMessage) throws Exception;
    String decrypt(String cipherMessage) throws Exception;
}
