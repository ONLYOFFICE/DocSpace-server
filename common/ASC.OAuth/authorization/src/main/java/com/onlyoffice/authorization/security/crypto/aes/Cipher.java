package com.onlyoffice.authorization.security.crypto.aes;

public interface Cipher {
    String encrypt(String plainMessage) throws Exception;
    String decrypt(String cipherMessage) throws Exception;
}
