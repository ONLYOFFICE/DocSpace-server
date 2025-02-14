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

package com.asc.registration.application.configuration;

import com.asc.registration.application.security.filter.BasicSignatureAuthenticationFilter;
import com.asc.registration.application.security.filter.RateLimiterFilter;
import com.asc.registration.application.security.provider.SignatureAuthenticationProvider;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.security.web.util.matcher.RequestMatcher;

/** The SecurityConfiguration class provides security configuration for the application. */
@Configuration
@EnableWebSecurity
@RequiredArgsConstructor
public class SecurityConfiguration {
  @Value("${server.port}")
  private int serverPort;

  @Value("${spring.application.web.api}")
  private String webApi;

  private final RateLimiterFilter rateLimiterFilter;
  private final BasicSignatureAuthenticationFilter basicSignatureAuthenticationFilter;
  private final SignatureAuthenticationProvider signatureAuthenticationProvider;

  /**
   * Configures the security filter chain for HTTP requests.
   *
   * @param http the HttpSecurity object for configuring security
   * @return the SecurityFilterChain object representing the configured security filter chain
   * @throws Exception if an error occurs during configuration
   */
  @Bean
  SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) throws Exception {
    return http.authorizeHttpRequests(
            authorizeRequests ->
                authorizeRequests
                    .requestMatchers(checkManagementPort())
                    .permitAll()
                    .requestMatchers(
                        String.format("%s/clients/*/public/info", webApi), "/docs", "/health/**")
                    .permitAll()
                    .requestMatchers(
                        String.format("%s/scopes", webApi),
                        String.format("%s/clients/.*?/info", webApi),
                        String.format("%s/clients/info", webApi),
                        String.format("%s/clients/consents", webApi),
                        String.format("%s/clients/*/revoke", webApi))
                    .hasAnyRole("ADMIN", "USER")
                    .anyRequest()
                    .hasRole("ADMIN"))
        .addFilterAt(basicSignatureAuthenticationFilter, UsernamePasswordAuthenticationFilter.class)
        .addFilterAfter(rateLimiterFilter, UsernamePasswordAuthenticationFilter.class)
        .authenticationProvider(signatureAuthenticationProvider)
        .csrf(AbstractHttpConfigurer::disable)
        .cors(AbstractHttpConfigurer::disable)
        .build();
  }

  /**
   * This method verifies whether a request port is equal to the management server port
   *
   * @return Returns a request matcher object with port comparison
   */
  private RequestMatcher checkManagementPort() {
    return (HttpServletRequest request) -> request.getLocalPort() != serverPort;
  }
}
