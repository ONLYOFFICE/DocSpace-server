package com.asc.authorization.application.configuration.security;

import com.asc.authorization.application.security.filters.AnonymousReplacerAuthenticationFilter;
import com.asc.authorization.application.security.filters.RateLimiterFilter;
import com.asc.authorization.application.security.oauth.providers.AscAuthenticationProvider;
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
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configurers.OAuth2AuthorizationServerConfigurer;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
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
  private final AscAuthenticationProvider authenticationProvider;

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
    OAuth2AuthorizationServerConfiguration.applyDefaultSecurity(http);

    http.getConfigurer(OAuth2AuthorizationServerConfigurer.class)
        .oidc(Customizer.withDefaults())
        .authorizationEndpoint(
            e -> {
              e.consentPage(formConfiguration.getConsent());
              e.authenticationProvider(authenticationProvider);
              e.authorizationResponseHandler(authenticationSuccessHandler);
              e.errorResponseHandler(authenticationFailureHandler);
            });

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
   * @param jwkSource the {@link JWKSource} of {@link SecurityContext}.
   * @return the {@link JwtEncoder} bean.
   */
  @Bean
  public JwtEncoder jwtEncoder(JWKSource<SecurityContext> jwkSource) {
    return new NimbusJwtEncoder(jwkSource);
  }

  /**
   * Creates the JWT decoder bean using the provided JWK source.
   *
   * @param jwkSource the {@link JWKSource} of {@link SecurityContext}.
   * @return the {@link JwtDecoder} bean.
   */
  @Bean
  public JwtDecoder jwtDecoder(JWKSource<SecurityContext> jwkSource) {
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
}
