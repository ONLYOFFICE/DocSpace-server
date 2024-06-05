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
public class AscOAuth2AuthorizationFormConfiguration {
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
        .formLogin(form -> form.loginPage(login).loginProcessingUrl(login).permitAll())
        .logout(AbstractHttpConfigurer::disable)
        .csrf(AbstractHttpConfigurer::disable)
        .cors(AbstractHttpConfigurer::disable)
        .build();
  }
}
