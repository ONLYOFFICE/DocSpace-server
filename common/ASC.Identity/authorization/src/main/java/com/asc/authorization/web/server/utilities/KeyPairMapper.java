package com.asc.authorization.web.server.utilities;

import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

import java.security.*;
import java.security.spec.InvalidKeySpecException;
import java.security.spec.PKCS8EncodedKeySpec;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;

@Slf4j
@Component
public class KeyPairMapper {
    public String toString(Key key) {
        return Base64.getEncoder().encodeToString(key.getEncoded());
    }

    public PublicKey toPublicKey(String key, String algorithm)
            throws NoSuchAlgorithmException, InvalidKeySpecException {
        MDC.put("key", key);
        MDC.put("algorithm", algorithm);
        log.debug("Converting a key string to a public key");
        MDC.clear();
        var decoded = Base64.getDecoder().decode(key);
        var spec = new X509EncodedKeySpec(decoded);
        var factory = KeyFactory.getInstance(algorithm);
        return factory.generatePublic(spec);
    }

    public PrivateKey toPrivateKey(String key, String algorithm)
            throws NoSuchAlgorithmException, InvalidKeySpecException {
        MDC.put("key", key);
        MDC.put("algorithm", algorithm);
        log.debug("Converting a key string to a private key");
        MDC.clear();
        var decoded = Base64.getDecoder().decode(key);
        var spec = new PKCS8EncodedKeySpec(decoded);
        var factory = KeyFactory.getInstance(algorithm);
        return factory.generatePrivate(spec);
    }
}
