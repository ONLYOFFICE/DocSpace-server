// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.authorization.application.security.oauth.jwks;

import com.asc.common.core.domain.value.KeyPairType;
import com.nimbusds.jose.jwk.JWK;
import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.spec.InvalidKeySpecException;

/**
 * Interface for generating and building JSON Web Keys (JWK) from cryptographic key pairs.
 *
 * <p>This interface defines methods for creating key pairs and constructing JWKs using either
 * {@link PublicKey} and {@link PrivateKey} objects or their Base64-encoded string representations.
 */
public interface JwksKeyPairGenerator {

  /**
   * Generates a cryptographic key pair.
   *
   * <p>This method creates a key pair using a specific algorithm determined by the implementation.
   *
   * @return the generated {@link KeyPair}.
   * @throws NoSuchAlgorithmException if the required algorithm is not available in the environment.
   */
  KeyPair generateKeyPair() throws NoSuchAlgorithmException;

  /**
   * Builds a JWK from the provided public and private key objects.
   *
   * <p>This method constructs a JWK using the provided {@link PublicKey} and {@link PrivateKey}.
   *
   * @param id the key ID to associate with the JWK.
   * @param publicKey the public key.
   * @param privateKey the private key.
   * @return the constructed {@link JWK}.
   */
  JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey);

  /**
   * Builds a JWK from the provided Base64-encoded public and private key strings.
   *
   * <p>This method decodes the Base64-encoded keys into {@link PublicKey} and {@link PrivateKey}
   * objects and constructs a JWK.
   *
   * @param id the key ID to associate with the JWK.
   * @param publicKey the Base64-encoded public key string.
   * @param privateKey the Base64-encoded private key string.
   * @return the constructed {@link JWK}.
   * @throws NoSuchAlgorithmException if the required algorithm is not available in the environment.
   * @throws InvalidKeySpecException if the provided key specifications are invalid.
   */
  JWK buildKey(String id, String publicKey, String privateKey)
      throws NoSuchAlgorithmException, InvalidKeySpecException;

  /**
   * Returns the type of key pair supported by the implementation.
   *
   * <p>This method identifies the type of cryptographic keys (e.g., RSA, EC) generated and handled
   * by the implementation.
   *
   * @return the {@link KeyPairType}.
   */
  KeyPairType type();
}
