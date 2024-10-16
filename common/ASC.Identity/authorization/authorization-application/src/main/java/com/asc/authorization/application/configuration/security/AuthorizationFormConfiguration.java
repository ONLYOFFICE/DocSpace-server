// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.application.configuration.security;

import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import lombok.Data;
import lombok.SneakyThrows;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.web.SecurityFilterChain;

/** Configuration class for setting up OAuth2 authorization form endpoints. */
@Data
@Configuration
@EnableWebSecurity
@ConfigurationProperties("spring.security.oauth2.server")
public class AuthorizationFormConfiguration {
  /** Endpoint for the login form. */
  private String login = "/oauth2/login";

  /** Endpoint for the consent form. */
  private String consent = "/oauth2/consent";

  /**
   * Configures the security filter chain, including form login and disabling unnecessary features.
   *
   * @param http the {@link HttpSecurity} to modify.
   * @return the {@link SecurityFilterChain} that is built.
   */
  @Bean
  @SneakyThrows
  @RateLimiter(name = "globalRateLimiter")
  SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) {
    return http.authorizeHttpRequests(
            authorizeRequests -> authorizeRequests.anyRequest().permitAll())
        .logout(AbstractHttpConfigurer::disable)
        .csrf(AbstractHttpConfigurer::disable)
        .cors(AbstractHttpConfigurer::disable)
        .build();
  }
}
