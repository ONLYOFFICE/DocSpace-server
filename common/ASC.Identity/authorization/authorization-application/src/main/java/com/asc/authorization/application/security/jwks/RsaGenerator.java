package com.asc.authorization.application.security.jwks;

import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.common.core.domain.value.KeyPairType;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.RSAKey;
import java.security.*;
import java.security.interfaces.RSAPrivateKey;
import java.security.interfaces.RSAPublicKey;
import java.security.spec.InvalidKeySpecException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Component;

/** Generates RSA key pairs and constructs JWKs. */
@Slf4j
@Component
@Qualifier("rsa")
@RequiredArgsConstructor
public class RsaGenerator implements JwksKeyPairGenerator {
  private final KeyPairMapper keyMapper;

  /**
   * Generates an RSA key pair.
   *
   * @return The generated RSA key pair.
   * @throws NoSuchAlgorithmException If the algorithm is not available in the environment.
   */
  public KeyPair generateKeyPair() throws NoSuchAlgorithmException {
    log.info("Generating RSA JWK key pair");

    try {
      var keyPairGenerator = KeyPairGenerator.getInstance("RSA");
      keyPairGenerator.initialize(2048);
      return keyPairGenerator.generateKeyPair();
    } catch (NoSuchAlgorithmException ex) {
      log.error("Could not generate a JWK key pair", ex);
      throw new NoSuchAlgorithmException("Failed to generate RSA key pair", ex);
    }
  }

  /**
   * Constructs a JWK from the given public and private keys.
   *
   * @param id The key ID.
   * @param publicKey The public key.
   * @param privateKey The private key.
   * @return The constructed JWK.
   */
  public JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey) {
    return new RSAKey.Builder((RSAPublicKey) publicKey)
        .privateKey((RSAPrivateKey) privateKey)
        .keyID(id)
        .build();
  }

  /**
   * Constructs a JWK from the given public and private key strings.
   *
   * @param id The key ID.
   * @param publicKey The public key string.
   * @param privateKey The private key string.
   * @return The constructed JWK.
   * @throws NoSuchAlgorithmException If the algorithm is not available in the environment.
   * @throws InvalidKeySpecException If the key specifications are invalid.
   */
  public JWK buildKey(String id, String publicKey, String privateKey)
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    var rsaPublicKey = (RSAPublicKey) keyMapper.toPublicKey(publicKey, "RSA");
    var rsaPrivateKey = (RSAPrivateKey) keyMapper.toPrivateKey(privateKey, "RSA");

    return new RSAKey.Builder(rsaPublicKey).privateKey(rsaPrivateKey).keyID(id).build();
  }

  /**
   * Returns the key pair type.
   *
   * @return The key pair type.
   */
  public KeyPairType type() {
    return KeyPairType.RSA;
  }
}
