// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.authorization.application.security.oauth.jwks;

import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.common.core.domain.value.KeyPairType;
import com.nimbusds.jose.jwk.Curve;
import com.nimbusds.jose.jwk.ECKey;
import com.nimbusds.jose.jwk.JWK;
import java.security.*;
import java.security.interfaces.ECPrivateKey;
import java.security.interfaces.ECPublicKey;
import java.security.spec.ECGenParameterSpec;
import java.security.spec.InvalidKeySpecException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

/**
 * Generates elliptic curve (EC) key pairs and constructs JSON Web Keys (JWKs).
 *
 * <p>This class provides methods for generating EC key pairs and building JWKs from key objects or
 * Base64-encoded key strings. It supports the "secp256r1" curve by default.
 */
@Slf4j
@Component(value = "ec")
@RequiredArgsConstructor
public class EcGenerator implements JwksKeyPairGenerator {
  private final KeyPairMapper keyMapper;

  /**
   * Generates an elliptic curve (EC) key pair.
   *
   * <p>The method uses the "secp256r1" curve to generate a key pair suitable for use in JSON Web
   * Keys (JWKs).
   *
   * @return the generated EC key pair.
   * @throws NoSuchAlgorithmException if the EC algorithm is not available in the current
   *     environment.
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
   * Constructs a JWK from the provided public and private key objects.
   *
   * <p>This method creates a JWK for the "secp256r1" curve using the given public and private keys.
   *
   * @param id the key ID to associate with the JWK.
   * @param publicKey the public key.
   * @param privateKey the private key.
   * @return the constructed {@link JWK}.
   */
  public JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey) {
    var ecPublicKey = (ECPublicKey) publicKey;
    var ecPrivateKey = (ECPrivateKey) privateKey;
    var curve = Curve.forECParameterSpec(ecPublicKey.getParams());
    return new ECKey.Builder(curve, ecPublicKey).privateKey(ecPrivateKey).keyID(id).build();
  }

  /**
   * Constructs a JWK from the provided Base64-encoded public and private key strings.
   *
   * <p>This method decodes the provided key strings into {@link PublicKey} and {@link PrivateKey}
   * objects and creates a JWK for the "secp256r1" curve.
   *
   * @param id the key ID to associate with the JWK.
   * @param publicKey the Base64-encoded public key string.
   * @param privateKey the Base64-encoded private key string.
   * @return the constructed {@link JWK}.
   * @throws NoSuchAlgorithmException if the EC algorithm is not available in the current
   *     environment.
   * @throws InvalidKeySpecException if the provided key specifications are invalid.
   */
  public JWK buildKey(String id, String publicKey, String privateKey)
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    var ecPublicKey = (ECPublicKey) keyMapper.toPublicKey(publicKey, "EC");
    var ecPrivateKey = (ECPrivateKey) keyMapper.toPrivateKey(privateKey, "EC");
    var curve = Curve.forECParameterSpec(ecPublicKey.getParams());
    return new ECKey.Builder(curve, ecPublicKey).privateKey(ecPrivateKey).keyID(id).build();
  }

  /**
   * Returns the type of key pair generated by this class.
   *
   * @return the {@link KeyPairType} indicating the EC key pair type.
   */
  public KeyPairType type() {
    return KeyPairType.EC;
  }
}
