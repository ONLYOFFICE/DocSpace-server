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

package com.asc.authorization.application.mapper;

import java.security.*;
import java.security.spec.InvalidKeySpecException;
import java.security.spec.PKCS8EncodedKeySpec;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

/**
 * Component for mapping between {@link Key} objects and their Base64-encoded string
 * representations.
 *
 * <p>This class provides methods for converting {@link PublicKey} and {@link PrivateKey} objects to
 * and from their Base64 string representations, as well as utilities for handling cryptographic
 * keys.
 */
@Slf4j
@Component
public class KeyPairMapper {
  /**
   * Converts a {@link Key} object to its Base64-encoded string representation.
   *
   * @param key the {@link Key} object to convert.
   * @return the Base64-encoded string representation of the key.
   */
  public String toString(Key key) {
    return Base64.getEncoder().encodeToString(key.getEncoded());
  }

  /**
   * Converts a Base64-encoded public key string to a {@link PublicKey} object.
   *
   * <p>This method decodes the Base64 string, parses it into an {@link X509EncodedKeySpec}, and
   * generates a {@link PublicKey} using the specified algorithm.
   *
   * @param key the Base64-encoded public key string.
   * @param algorithm the algorithm used for the key (e.g., "RSA").
   * @return the constructed {@link PublicKey} object.
   * @throws NoSuchAlgorithmException if the specified algorithm is not available.
   * @throws InvalidKeySpecException if the key specification is invalid.
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
   * Converts a Base64-encoded private key string to a {@link PrivateKey} object.
   *
   * <p>This method decodes the Base64 string, parses it into a {@link PKCS8EncodedKeySpec}, and
   * generates a {@link PrivateKey} using the specified algorithm.
   *
   * @param key the Base64-encoded private key string.
   * @param algorithm the algorithm used for the key (e.g., "RSA").
   * @return the constructed {@link PrivateKey} object.
   * @throws NoSuchAlgorithmException if the specified algorithm is not available.
   * @throws InvalidKeySpecException if the key specification is invalid.
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
