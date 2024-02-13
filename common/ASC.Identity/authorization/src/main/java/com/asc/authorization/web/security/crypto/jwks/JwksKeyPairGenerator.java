/**
 *
 */
package com.asc.authorization.web.security.crypto.jwks;

import com.asc.authorization.core.entities.KeyPairType;
import com.nimbusds.jose.jwk.JWK;

import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.spec.InvalidKeySpecException;

/**
 *
 */
public interface JwksKeyPairGenerator {
    /**
     *
     * @return
     * @throws NoSuchAlgorithmException
     */
    KeyPair generateKeyPair() throws NoSuchAlgorithmException;

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     */
    JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey);

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     */
    JWK buildKey(String id, String publicKey, String privateKey) throws NoSuchAlgorithmException, InvalidKeySpecException;

    /**
     * 
     * @return
     */
    KeyPairType type();
}
