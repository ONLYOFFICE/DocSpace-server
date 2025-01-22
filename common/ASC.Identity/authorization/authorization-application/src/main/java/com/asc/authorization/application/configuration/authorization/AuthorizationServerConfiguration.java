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

import com.asc.authorization.application.security.filter.BasicSignatureAuthenticationFilter;
import com.asc.authorization.application.security.filter.RateLimiterFilter;
import com.asc.authorization.application.security.oauth.converter.PersonalAccessTokenAuthenticationConverter;
import com.asc.authorization.application.security.oauth.provider.PersonalAccessTokenAuthenticationProvider;
import com.asc.authorization.application.security.oauth.provider.TokenIntrospectionAuthenticationProvider;
import com.asc.authorization.application.security.provider.SignatureAuthenticationProvider;
import jakarta.servlet.RequestDispatcher;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.crypto.password.NoOpPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configurers.OAuth2AuthorizationServerConfigurer;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.access.channel.ChannelProcessingFilter;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.security.web.authentication.logout.LogoutFilter;
import org.springframework.security.web.util.matcher.AntPathRequestMatcher;

/**
 * Configuration class for setting up the OAuth2 Authorization Server.
 *
 * <p>This configuration class handles the security settings, endpoint configurations, and various
 * authentication-related components for the OAuth2 Authorization Server.
 */
@Configuration
@RequiredArgsConstructor
public class AuthorizationServerConfiguration {
  private final AuthorizationFormConfiguration formConfiguration;

  private final RateLimiterFilter rateLimiterFilter;
  private final BasicSignatureAuthenticationFilter authenticationFilter;

  private final PersonalAccessTokenAuthenticationConverter
      personalAccessTokenAuthenticationConverter;

  private final PersonalAccessTokenAuthenticationProvider personalAccessTokenAuthenticationProvider;
  private final SignatureAuthenticationProvider codeAuthenticationProvider;
  private final TokenIntrospectionAuthenticationProvider tokenIntrospectionAuthenticationProvider;

  private final AuthenticationSuccessHandler authenticationSuccessHandler;
  private final AuthenticationFailureHandler authenticationFailureHandler;

  /**
   * Configures the security filter chain for the authorization server.
   *
   * <p>This method sets up the endpoint-specific security rules, authentication providers, filters,
   * and exception handling for the OAuth2 Authorization Server.
   *
   * @param http the {@link HttpSecurity} object used to configure security settings.
   * @return the constructed {@link SecurityFilterChain}.
   */
  @Bean
  @Order(Ordered.HIGHEST_PRECEDENCE)
  @SneakyThrows
  public SecurityFilterChain authorizationServerSecurityFilterChain(HttpSecurity http) {
    var authorizationServerConfigurer = new OAuth2AuthorizationServerConfigurer();
    var endpointsMatcher = authorizationServerConfigurer.getEndpointsMatcher();

    http.securityMatcher(endpointsMatcher)
        .authorizeHttpRequests(
            authorize -> {
              authorize.requestMatchers("oauth2/introspect").permitAll();
              authorize.anyRequest().authenticated();
            })
        .csrf(csrf -> csrf.ignoringRequestMatchers(endpointsMatcher))
        .apply(authorizationServerConfigurer);

    http.getConfigurer(OAuth2AuthorizationServerConfigurer.class)
        .oidc(Customizer.withDefaults())
        .tokenEndpoint(
            t ->
                t.accessTokenRequestConverters(
                        converters -> converters.add(personalAccessTokenAuthenticationConverter))
                    .authenticationProviders(
                        providers -> providers.add(personalAccessTokenAuthenticationProvider)))
        .authorizationEndpoint(
            e -> {
              e.consentPage(formConfiguration.getConsent());
              e.authenticationProvider(codeAuthenticationProvider);
              e.authorizationResponseHandler(authenticationSuccessHandler);
              e.errorResponseHandler(authenticationFailureHandler);
            })
        .tokenIntrospectionEndpoint(
            i -> i.authenticationProvider(tokenIntrospectionAuthenticationProvider));

    http.exceptionHandling(
        e ->
            e.defaultAuthenticationEntryPointFor(
                (request, response, authException) -> {
                  RequestDispatcher dispatcher =
                      request.getRequestDispatcher(formConfiguration.getLogin());
                  dispatcher.forward(request, response);
                },
                new AntPathRequestMatcher(formConfiguration.getLogin())));
    http.addFilterBefore(rateLimiterFilter, ChannelProcessingFilter.class);
    http.addFilterBefore(authenticationFilter, LogoutFilter.class);

    http.cors(AbstractHttpConfigurer::disable);
    http.csrf(AbstractHttpConfigurer::disable);

    return http.build();
  }

  /**
   * Configures client settings for the OAuth2 Authorization Server.
   *
   * <p>The client settings determine whether authorization consent and proof keys are required for
   * client interactions.
   *
   * @return the configured {@link ClientSettings} bean.
   */
  @Bean
  public ClientSettings clientSettings() {
    return ClientSettings.builder()
        .requireAuthorizationConsent(true)
        .requireProofKey(false)
        .build();
  }

  /**
   * Configures the password encoder.
   *
   * <p>In this implementation, a NoOpPasswordEncoder is used, which does not apply any encoding.
   * Since we rely on X-Signature instead of users, we do not really need an encoder.
   *
   * @return the {@link PasswordEncoder} bean.
   */
  @Bean
  public PasswordEncoder passwordEncoder() {
    return NoOpPasswordEncoder.getInstance();
  }
}
