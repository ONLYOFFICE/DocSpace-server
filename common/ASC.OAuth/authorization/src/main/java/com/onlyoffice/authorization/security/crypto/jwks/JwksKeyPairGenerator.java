/**
 *
 */
package com.onlyoffice.authorization.security.crypto.jwks;

import com.nimbusds.jose.jwk.JWK;

import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;

/**
 *
 */
public interface JwksKeyPairGenerator {
    JWK generateKey() throws NoSuchAlgorithmException;
    KeyPair generateKeyPair() throws NoSuchAlgorithmException;
}
