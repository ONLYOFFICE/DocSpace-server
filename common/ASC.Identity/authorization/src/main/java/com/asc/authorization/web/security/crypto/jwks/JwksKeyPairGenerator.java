/**
 *
 */
package com.asc.authorization.web.security.crypto.jwks;

import com.nimbusds.jose.jwk.JWK;

import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;

/**
 *
 */
public interface JwksKeyPairGenerator {
    /**
     *
     * @return
     * @throws NoSuchAlgorithmException
     */
    JWK generateKey() throws NoSuchAlgorithmException;

    /**
     *
     * @return
     * @throws NoSuchAlgorithmException
     */
    KeyPair generateKeyPair() throws NoSuchAlgorithmException;
}
