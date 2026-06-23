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

package com.asc.authorization.application.configuration.cryptography;

import com.asc.authorization.application.security.oauth.generator.PrefixedRefreshTokenGenerator;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.env.Environment;
import org.springframework.security.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration;
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
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
  @Value("${spring.application.region}")
  private String region;

  private final Environment environment;

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
   * <p>The refresh token generator is configured with a region-based prefix for multi-region
   * support.
   *
   * @return a configured {@link OAuth2TokenGenerator} instance.
   */
  @Bean
  public OAuth2TokenGenerator<? extends OAuth2Token> tokenGenerator() {
    var generator = new JwtGenerator(jwtEncoder());
    generator.setJwtCustomizer(jwtCustomizer);
    var accessTokenGenerator = new OAuth2AccessTokenGenerator();
    var refreshTokenGenerator = new PrefixedRefreshTokenGenerator(environment, region);
    return new DelegatingOAuth2TokenGenerator(
        generator, accessTokenGenerator, refreshTokenGenerator);
  }
}
