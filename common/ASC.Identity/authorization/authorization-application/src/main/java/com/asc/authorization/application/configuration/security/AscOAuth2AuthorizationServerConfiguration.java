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

import com.asc.authorization.application.security.filters.AnonymousReplacerAuthenticationFilter;
import com.asc.authorization.application.security.filters.RateLimiterFilter;
import com.asc.authorization.application.security.oauth.converters.PersonalAccessTokenAuthenticationConverter;
import com.asc.authorization.application.security.oauth.providers.AscCodeAuthenticationProvider;
import com.asc.authorization.application.security.oauth.providers.AscPersonalAccessTokenAuthenticationProvider;
import com.asc.authorization.application.security.oauth.providers.AscTokenIntrospectionAuthenticationProvider;
import com.asc.common.application.client.AscApiClient;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
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
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configurers.OAuth2AuthorizationServerConfigurer;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.oauth2.server.authorization.token.*;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.access.channel.ChannelProcessingFilter;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.security.web.authentication.logout.LogoutFilter;
import org.springframework.security.web.util.matcher.AntPathRequestMatcher;

/** Configuration class for setting up the OAuth2 Authorization Server. */
@Configuration
@RequiredArgsConstructor
public class AscOAuth2AuthorizationServerConfiguration {
  private final AscOAuth2AuthorizationFormConfiguration formConfiguration;

  private final AscApiClient ascApiClient;
  private final AuditMessagePublisher auditMessagePublisher;

  private final OAuth2TokenCustomizer<JwtEncodingContext> jwtCustomizer;
  private final JWKSource<SecurityContext> jwkSource;

  private final OAuth2AuthorizationService authorizationService;

  private final AscCodeAuthenticationProvider codeAuthenticationProvider;
  private final AscTokenIntrospectionAuthenticationProvider
      tokenIntrospectionAuthenticationProvider;

  private final AuthenticationSuccessHandler authenticationSuccessHandler;
  private final AuthenticationFailureHandler authenticationFailureHandler;

  private final RateLimiterFilter rateLimiterFilter;
  private final AnonymousReplacerAuthenticationFilter authenticationFilter;

  /**
   * Configures the security filter chain for the authorization server.
   *
   * @param http the {@link HttpSecurity} to modify.
   * @return the {@link SecurityFilterChain} that is built.
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
                        converters ->
                            converters.add(
                                new PersonalAccessTokenAuthenticationConverter(ascApiClient)))
                    .authenticationProviders(
                        providers ->
                            providers.add(
                                new AscPersonalAccessTokenAuthenticationProvider(
                                    auditMessagePublisher,
                                    authorizationService,
                                    tokenGenerator()))))
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
   * Creates the client settings bean.
   *
   * @return the {@link ClientSettings} bean.
   */
  @Bean
  public ClientSettings clientSettings() {
    return ClientSettings.builder()
        .requireAuthorizationConsent(true)
        .requireProofKey(false)
        .build();
  }

  /**
   * Creates the JWT encoder bean using the provided JWK source.
   *
   * @return the {@link JwtEncoder} bean.
   */
  @Bean
  public JwtEncoder jwtEncoder() {
    return new NimbusJwtEncoder(jwkSource);
  }

  /**
   * Creates the JWT decoder bean using the provided JWK source.
   *
   * @return the {@link JwtDecoder} bean.
   */
  @Bean
  public JwtDecoder jwtDecoder() {
    return OAuth2AuthorizationServerConfiguration.jwtDecoder(jwkSource);
  }

  /**
   * Creates the password encoder bean.
   *
   * @return the {@link PasswordEncoder} bean.
   */
  @Bean
  public PasswordEncoder passwordEncoder() {
    return NoOpPasswordEncoder.getInstance();
  }

  /**
   * Creates the token generator bean.
   *
   * @return the {@link OAuth2TokenGenerator} bean.
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
