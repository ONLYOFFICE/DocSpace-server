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

package com.asc.authorization.application.security.service;

import com.asc.common.utilities.crypto.MachinePseudoKeys;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import javax.crypto.spec.SecretKeySpec;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.oauth2.jose.jws.MacAlgorithm;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtException;
import org.springframework.security.oauth2.jwt.NimbusJwtDecoder;
import org.springframework.stereotype.Component;

/**
 * Service for validating and decoding JWT tokens.
 *
 * <p>This service uses a secret-based symmetric key to validate JWT tokens and parse their claims
 * into a strongly typed object. It supports deserialization of claims using Jackson with support
 * for Java 8 time features.
 */
@Component("authorizationSignatureService")
public class SignatureService {
  private final JwtDecoder jwtDecoder;
  private final ObjectMapper objectMapper;

  /**
   * Constructs a {@link SignatureService} with the provided signing secret.
   *
   * @param secret the secret key used for signing and validating JWT tokens, provided via the
   *     `application.signingSecret` configuration property.
   */
  public SignatureService(@Value("${spring.application.signature.secret}") String secret) {
    var machineKeyGenerator = new MachinePseudoKeys(secret);
    var secretKey =
        new SecretKeySpec(
            machineKeyGenerator.getMachineConstant(256), MacAlgorithm.HS256.getName());
    jwtDecoder = NimbusJwtDecoder.withSecretKey(secretKey).build();
    objectMapper =
        new ObjectMapper()
            .registerModule(new JavaTimeModule())
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
  }

  /**
   * Validates a JWT token and parses its claims into a specified type.
   *
   * <p>This method decodes the JWT token, extracts its claims as JSON, and deserializes the claims
   * into an instance of the specified class.
   *
   * @param token the JWT token to validate and decode.
   * @param clazz the class type to which the claims will be deserialized.
   * @param <T> the type of the object to return.
   * @return an instance of the specified class containing the parsed claims.
   * @throws RuntimeException if the token validation or deserialization fails.
   */
  public <T> T validate(String token, Class<T> clazz) {
    try {
      var jwt = jwtDecoder.decode(token);
      var claimsJson = objectMapper.writeValueAsString(jwt.getClaims());
      return objectMapper.readValue(claimsJson, clazz);
    } catch (JwtException | JsonProcessingException e) {
      throw new RuntimeException("Failed to validate token", e);
    }
  }
}
