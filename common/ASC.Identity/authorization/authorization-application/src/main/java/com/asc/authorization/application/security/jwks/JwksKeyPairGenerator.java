package com.asc.authorization.application.security.jwks;

import com.asc.common.core.domain.value.KeyPairType;
import com.nimbusds.jose.jwk.JWK;
import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.spec.InvalidKeySpecException;

/** Interface for generating and building JSON Web Keys (JWK) for key pairs. */
public interface JwksKeyPairGenerator {

  /**
   * Generates a key pair using a specified algorithm.
   *
   * @return the generated KeyPair.
   * @throws NoSuchAlgorithmException if the algorithm is not available.
   */
  KeyPair generateKeyPair() throws NoSuchAlgorithmException;

  /**
   * Builds a JWK from the given public and private keys.
   *
   * @param id the key ID.
   * @param publicKey the public key.
   * @param privateKey the private key.
   * @return the built JWK.
   */
  JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey);

  /**
   * Builds a JWK from the given public and private key strings.
   *
   * @param id the key ID.
   * @param publicKey the public key string.
   * @param privateKey the private key string.
   * @return the built JWK.
   * @throws NoSuchAlgorithmException if the algorithm is not available.
   * @throws InvalidKeySpecException if the key spec is invalid.
   */
  JWK buildKey(String id, String publicKey, String privateKey)
      throws NoSuchAlgorithmException, InvalidKeySpecException;

  /**
   * Returns the type of key pair.
   *
   * @return the KeyPairType.
   */
  KeyPairType type();
}
