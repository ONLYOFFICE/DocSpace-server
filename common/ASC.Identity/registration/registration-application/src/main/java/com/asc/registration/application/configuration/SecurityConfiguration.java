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

package com.asc.registration.application.configuration;

import com.asc.registration.application.security.filter.BasicSignatureAuthenticationFilter;
import com.asc.registration.application.security.filter.RateLimiterFilter;
import com.asc.registration.application.security.provider.SignatureAuthenticationProvider;
import jakarta.servlet.http.HttpServletRequest;
import java.util.Optional;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.annotation.Order;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.security.web.util.matcher.RequestMatcher;

/** The SecurityConfiguration class provides security configuration for the application. */
@Configuration
@EnableWebSecurity
@EnableMethodSecurity
public class SecurityConfiguration {
  @Value("${server.port}")
  private int serverPort;

  @Value("${spring.application.web.api}")
  private String webApi;

  private final Optional<RateLimiterFilter> rateLimiterFilter;
  private final BasicSignatureAuthenticationFilter basicSignatureAuthenticationFilter;
  private final SignatureAuthenticationProvider signatureAuthenticationProvider;

  public SecurityConfiguration(
      @Qualifier("registrationRateLimiterFilter") Optional<RateLimiterFilter> rateLimiterFilter,
      @Qualifier("registrationBasicSignatureAuthenticationFilter")
          BasicSignatureAuthenticationFilter basicSignatureAuthenticationFilter,
      @Qualifier("registrationSignatureAuthenticationProvider")
          SignatureAuthenticationProvider signatureAuthenticationProvider) {
    this.rateLimiterFilter = rateLimiterFilter;
    this.basicSignatureAuthenticationFilter = basicSignatureAuthenticationFilter;
    this.signatureAuthenticationProvider = signatureAuthenticationProvider;
  }

  /**
   * Configures the security filter chain for HTTP requests.
   *
   * @param http the HttpSecurity object for configuring security
   * @return the SecurityFilterChain object representing the configured security filter chain
   * @throws Exception if an error occurs during configuration
   */
  @Order(2)
  @Bean("registrationSecurityFilterChain")
  SecurityFilterChain registrationSecurityFilterChain(HttpSecurity http) throws Exception {
    var httpSecurity =
        http.authorizeHttpRequests(
                authorizeRequests ->
                    authorizeRequests
                        .requestMatchers(checkManagementPort())
                        .permitAll()
                        .requestMatchers(
                            String.format("%s/clients/*/public/info", webApi),
                            "/docs**",
                            "/health/**")
                        .permitAll()
                        .anyRequest()
                        .authenticated())
            .addFilterAt(
                basicSignatureAuthenticationFilter, UsernamePasswordAuthenticationFilter.class)
            .authenticationProvider(signatureAuthenticationProvider)
            .csrf(AbstractHttpConfigurer::disable)
            .cors(AbstractHttpConfigurer::disable);

    rateLimiterFilter.ifPresent(
        filter -> httpSecurity.addFilterAfter(filter, UsernamePasswordAuthenticationFilter.class));

    return httpSecurity.build();
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
