package com.asc.registration.application.configuration;

import com.asc.registration.application.security.AscCookieAuthenticationFilter;
import com.asc.registration.application.security.RateLimiterFilter;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

/** The SecurityConfiguration class provides security configuration for the application. */
@Configuration
@EnableWebSecurity
@RequiredArgsConstructor
public class SecurityConfiguration {
  private final RateLimiterFilter rateLimiterFilter;
  private final AscCookieAuthenticationFilter ascCookieAuthenticationFilter;

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
            authorizeRequests -> authorizeRequests.anyRequest().permitAll())
        .addFilterAt(ascCookieAuthenticationFilter, UsernamePasswordAuthenticationFilter.class)
        .addFilterAfter(rateLimiterFilter, UsernamePasswordAuthenticationFilter.class)
        .csrf(AbstractHttpConfigurer::disable)
        .cors(AbstractHttpConfigurer::disable)
        .build();
  }
}
