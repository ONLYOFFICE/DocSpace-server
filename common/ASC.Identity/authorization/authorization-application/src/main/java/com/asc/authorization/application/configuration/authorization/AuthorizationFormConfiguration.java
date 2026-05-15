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

package com.asc.authorization.application.configuration.authorization;

import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import java.util.List;
import lombok.Data;
import lombok.SneakyThrows;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.annotation.Order;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.CorsConfigurationSource;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;

/**
 * Configuration class for setting up OAuth2 authorization form endpoints.
 *
 * <p>This class defines and configures endpoints for the login and consent forms used in the OAuth2
 * authorization process. It also sets up the security filter chain to manage security settings for
 * form endpoints.
 */
@Data
@Configuration
@EnableWebSecurity
public class AuthorizationFormConfiguration {
  /** The endpoint for the login form. Default value: {@code "/oauth2/login"}. */
  private String login = "/oauth2/login";

  /** The endpoint for the consent form. Default value: {@code "/oauth2/consent"}. */
  private String consent = "/oauth2/consent";

  /**
   * Configures the security filter chain for form endpoints.
   *
   * <p>This method defines security settings such as: - Permitting all requests to any endpoint. -
   * Disabling unnecessary features like logout, CSRF protection, and CORS since we rely on external
   * authorization. - Enforcing a global rate limiter using Resilience4j's {@code @RateLimiter}.
   *
   * @param http the {@link HttpSecurity} object used to configure security settings.
   * @return the constructed {@link SecurityFilterChain}.
   */
  @Order(1)
  @SneakyThrows
  @RateLimiter(name = "globalRateLimiter")
  @Bean("authorizationSecurityFilterChain")
  SecurityFilterChain authorizationSecurityFilterChain(HttpSecurity http) {
    return http.securityMatcher("/oauth2/**", "/.well-known/**", "/connect/**", "/login/**")
        .authorizeHttpRequests(authorizeRequests -> authorizeRequests.anyRequest().permitAll())
        .logout(AbstractHttpConfigurer::disable)
        .csrf(AbstractHttpConfigurer::disable)
        .cors(c -> c.configurationSource(corsConfigurationSource()))
        .build();
  }

  /**
   * Creates and configures a CORS (Cross-Origin Resource Sharing) configuration source. This
   * configuration should be acceptable since we fully rely on signatures
   *
   * <p>This method sets up a permissive CORS configuration that allows:
   *
   * <ul>
   *   <li>All origins ({@code "*"}) to access the endpoints
   *   <li>All HTTP methods ({@code "*"}) including GET, POST, PUT, DELETE, etc.
   *   <li>All headers ({@code "*"}) in cross-origin requests
   *   <li>Preflight request caching for 1 hour (3600 seconds)
   * </ul>
   *
   * <p>The configuration is applied to all URL patterns ({@code "/**"}) within the application.
   *
   * @return a {@link CorsConfigurationSource} that provides CORS configuration for all endpoints
   * @see CorsConfiguration
   * @see UrlBasedCorsConfigurationSource
   */
  CorsConfigurationSource corsConfigurationSource() {
    var configuration = new CorsConfiguration();
    configuration.setAllowedOrigins(List.of("*"));
    configuration.setAllowedMethods(List.of("*"));
    configuration.setAllowedHeaders(List.of("*"));
    configuration.setMaxAge(3600L);

    var source = new UrlBasedCorsConfigurationSource();
    source.registerCorsConfiguration("/**", configuration);
    return source;
  }
}
