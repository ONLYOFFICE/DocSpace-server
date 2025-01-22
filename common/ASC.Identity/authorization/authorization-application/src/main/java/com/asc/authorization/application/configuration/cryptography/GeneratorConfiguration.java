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

package com.asc.authorization.application.configuration.cryptography;

import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration;
import org.springframework.security.oauth2.server.authorization.token.*;

/**
 * Configuration class for setting up token generation and processing for the OAuth2 Authorization
 * Server.
 *
 * <p>This class provides beans for encoding, decoding, and generating tokens using JSON Web Tokens
 * (JWTs). It utilizes the Nimbus library for JWT operations and supports token customization.
 */
@Configuration
@RequiredArgsConstructor
public class GeneratorConfiguration {
  private final JWKSource<SecurityContext> jwkSource;
  private final OAuth2TokenCustomizer<JwtEncodingContext> jwtCustomizer;

  /**
   * Creates the {@link JwtEncoder} bean.
   *
   * <p>This encoder uses the provided JSON Web Key (JWK) source to sign and encode JWTs.
   *
   * @return a configured {@link JwtEncoder} instance.
   */
  @Bean
  public JwtEncoder jwtEncoder() {
    return new NimbusJwtEncoder(jwkSource);
  }

  /**
   * Creates the {@link JwtDecoder} bean.
   *
   * <p>This decoder uses the provided JWK source to verify and decode JWTs.
   *
   * @return a configured {@link JwtDecoder} instance.
   */
  @Bean
  public JwtDecoder jwtDecoder() {
    return OAuth2AuthorizationServerConfiguration.jwtDecoder(jwkSource);
  }

  /**
   * Creates the {@link OAuth2TokenGenerator} bean for token generation.
   *
   * <p>This generator is responsible for creating OAuth2 tokens, including access tokens, refresh
   * tokens, and JWTs. It integrates a custom JWT encoder and supports token customization via the
   * provided {@link OAuth2TokenCustomizer}.
   *
   * @return a configured {@link OAuth2TokenGenerator} instance.
   */
  @Bean
  public OAuth2TokenGenerator<? extends OAuth2Token> tokenGenerator() {
    var generator = new JwtGenerator(jwtEncoder());
    generator.setJwtCustomizer(jwtCustomizer);
    var accessTokenGenerator = new OAuth2AccessTokenGenerator();
    var refreshTokenGenerator = new OAuth2RefreshTokenGenerator();
    return new DelegatingOAuth2TokenGenerator(
        generator, accessTokenGenerator, refreshTokenGenerator);
  }
}
