/**
 *
 */
package com.asc.authorization.web.security.crypto.jwks;

import com.asc.authorization.core.entities.KeyPairType;
import com.asc.authorization.web.server.utilities.KeyPairMapper;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.RSAKey;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Component;

import java.security.*;
import java.security.interfaces.RSAPublicKey;
import java.security.spec.InvalidKeySpecException;

/**
 *
 */
@Slf4j
@Component
@Qualifier("rsa")
@RequiredArgsConstructor
public class RSAGenerator implements JwksKeyPairGenerator {
    private final KeyPairMapper keyMapper;

    /**
     *
     * @return
     * @throws NoSuchAlgorithmException
     */
    public KeyPair generateKeyPair() throws NoSuchAlgorithmException {
        log.info("Generating rsa jwks key pair");

        KeyPairGenerator keyPairGenerator = KeyPairGenerator.getInstance("RSA");
        keyPairGenerator.initialize(2048);
        return keyPairGenerator.generateKeyPair();
    }

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     */
    public JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey) {
        return new RSAKey.Builder((RSAPublicKey) publicKey)
                .privateKey(privateKey)
                .keyID(id)
                .build();
    }

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     * @throws NoSuchAlgorithmException
     * @throws InvalidKeySpecException
     */
    public JWK buildKey(String id, String publicKey, String privateKey)
            throws NoSuchAlgorithmException, InvalidKeySpecException {
        return new RSAKey.Builder((RSAPublicKey) keyMapper.toPublicKey(publicKey, "RSA"))
                .privateKey(keyMapper.toPrivateKey(privateKey, "RSA"))
                .keyID(id)
                .build();
    }

    /**
     *
     * @return
     */
    public KeyPairType type() {
        return KeyPairType.RSA;
    }
}
