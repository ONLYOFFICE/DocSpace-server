package com.asc.authorization.application.security.jwks;

import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.common.core.domain.value.KeyPairType;
import com.nimbusds.jose.jwk.Curve;
import com.nimbusds.jose.jwk.ECKey;
import com.nimbusds.jose.jwk.JWK;
import java.security.*;
import java.security.interfaces.ECPrivateKey;
import java.security.interfaces.ECPublicKey;
import java.security.spec.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Primary;
import org.springframework.stereotype.Component;

/** Generates elliptic curve key pairs and constructs JWKs. */
@Slf4j
@Primary
@Component
@Qualifier("ec")
@RequiredArgsConstructor
public class EcGenerator implements JwksKeyPairGenerator {
  private final KeyPairMapper keyMapper;

  /**
   * Generates an elliptic curve key pair.
   *
   * @return The generated elliptic curve key pair.
   * @throws NoSuchAlgorithmException If the algorithm is not available in the environment.
   */
  public KeyPair generateKeyPair() throws NoSuchAlgorithmException {
    log.info("Generating elliptic curve JWK key pair");

    try {
      var keyPairGenerator = KeyPairGenerator.getInstance("EC");
      keyPairGenerator.initialize(new ECGenParameterSpec("secp256r1"));
      return keyPairGenerator.generateKeyPair();
    } catch (NoSuchAlgorithmException | InvalidAlgorithmParameterException ex) {
      log.error("Could not generate a JWK key pair", ex);
      throw new NoSuchAlgorithmException(ex);
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
    var ecPublicKey = (ECPublicKey) publicKey;
    var ecPrivateKey = (ECPrivateKey) privateKey;
    var curve = Curve.forECParameterSpec(ecPublicKey.getParams());
    return new ECKey.Builder(curve, ecPublicKey).privateKey(ecPrivateKey).keyID(id).build();
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
    var ecPublicKey = (ECPublicKey) keyMapper.toPublicKey(publicKey, "EC");
    var ecPrivateKey = (ECPrivateKey) keyMapper.toPrivateKey(privateKey, "EC");
    var curve = Curve.forECParameterSpec(ecPublicKey.getParams());
    return new ECKey.Builder(curve, ecPublicKey).privateKey(ecPrivateKey).keyID(id).build();
  }

  /**
   * Returns the key pair type.
   *
   * @return The key pair type.
   */
  public KeyPairType type() {
    return KeyPairType.EC;
  }
}
