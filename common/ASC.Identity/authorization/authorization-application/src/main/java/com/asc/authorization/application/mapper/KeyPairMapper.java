package com.asc.authorization.application.mapper;

import java.security.*;
import java.security.spec.InvalidKeySpecException;
import java.security.spec.PKCS8EncodedKeySpec;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

/** Component for mapping between Key objects and their string representations. */
@Slf4j
@Component
public class KeyPairMapper {
  /**
   * Converts a Key object to its Base64 encoded string representation.
   *
   * @param key the Key object.
   * @return the Base64 encoded string representation of the key.
   */
  public String toString(Key key) {
    return Base64.getEncoder().encodeToString(key.getEncoded());
  }

  /**
   * Converts a Base64 encoded public key string to a PublicKey object.
   *
   * @param key the Base64 encoded public key string.
   * @param algorithm the algorithm of the key.
   * @return the PublicKey object.
   * @throws NoSuchAlgorithmException if the algorithm is not available.
   * @throws InvalidKeySpecException if the key spec is invalid.
   */
  public PublicKey toPublicKey(String key, String algorithm)
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    MDC.put("key", key);
    MDC.put("algorithm", algorithm);
    log.debug("Converting a key string to a public key");
    var decoded = Base64.getDecoder().decode(key);
    var spec = new X509EncodedKeySpec(decoded);
    var factory = KeyFactory.getInstance(algorithm);
    MDC.clear();
    return factory.generatePublic(spec);
  }

  /**
   * Converts a Base64 encoded private key string to a PrivateKey object.
   *
   * @param key the Base64 encoded private key string.
   * @param algorithm the algorithm of the key.
   * @return the PrivateKey object.
   * @throws NoSuchAlgorithmException if the algorithm is not available.
   * @throws InvalidKeySpecException if the key spec is invalid.
   */
  public PrivateKey toPrivateKey(String key, String algorithm)
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    MDC.put("key", key);
    MDC.put("algorithm", algorithm);
    log.debug("Converting a key string to a private key");
    var decoded = Base64.getDecoder().decode(key);
    var spec = new PKCS8EncodedKeySpec(decoded);
    var factory = KeyFactory.getInstance(algorithm);
    MDC.clear();
    return factory.generatePrivate(spec);
  }
}
