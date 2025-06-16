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

package com.asc.authorization.application.configuration.authorization;

import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import java.util.List;
import lombok.Data;
import lombok.SneakyThrows;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
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
  @Bean
  @SneakyThrows
  @RateLimiter(name = "globalRateLimiter")
  SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) {
    return http.authorizeHttpRequests(
            authorizeRequests -> authorizeRequests.anyRequest().permitAll())
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
