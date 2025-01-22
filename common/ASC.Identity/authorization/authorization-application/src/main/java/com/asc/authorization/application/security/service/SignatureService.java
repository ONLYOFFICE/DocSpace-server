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

package com.asc.authorization.application.security.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import java.nio.charset.StandardCharsets;
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
@Component
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
    var secretKey =
        new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), MacAlgorithm.HS256.getName());
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
