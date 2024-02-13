/**
 *
 */
package com.asc.authorization.configuration;

import com.asc.authorization.web.security.filters.AnonymousReplacerAuthenticationFilter;
import com.asc.authorization.web.security.filters.DistributedRateLimiterFilter;
import com.asc.authorization.web.security.oauth.providers.AscAuthenticationProvider;
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
import org.springframework.security.crypto.password.NoOpPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configurers.OAuth2AuthorizationServerConfigurer;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.security.web.authentication.logout.LogoutFilter;
import org.springframework.security.web.util.matcher.AntPathRequestMatcher;

/**
 *
 */
@Configuration
@RequiredArgsConstructor
public class OAuth2AuthorizationServerConfiguration {
    private final OAuth2SecurityFormConfiguration formConfiguration;
    private final AscAuthenticationProvider authenticationProvider;

    private final AuthenticationSuccessHandler authenticationSuccessHandler;
    private final AuthenticationFailureHandler authenticationFailureHandler;

    private final DistributedRateLimiterFilter distributedRateLimiterFilter;
    private final AnonymousReplacerAuthenticationFilter authenticationFilter;

    /**
     *
     * @param http
     * @return
     */
    @Bean
    @Order(Ordered.HIGHEST_PRECEDENCE)
    @SneakyThrows
    public SecurityFilterChain authorizationServerSecurityFilterChain(HttpSecurity http) {
        org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration.applyDefaultSecurity(http);

        http.getConfigurer(OAuth2AuthorizationServerConfigurer.class)
                .oidc(Customizer.withDefaults())
                .authorizationEndpoint(e -> {
                    e.consentPage(formConfiguration.getConsent());
                    e.authenticationProvider(authenticationProvider);
                    e.authorizationResponseHandler(authenticationSuccessHandler);
                    e.errorResponseHandler(authenticationFailureHandler);
                });

        http.exceptionHandling(e -> e.defaultAuthenticationEntryPointFor((request, response, authException) -> {
            RequestDispatcher dispatcher = request.getRequestDispatcher(formConfiguration.getLogin());
            dispatcher.forward(request, response);
        }, new AntPathRequestMatcher(formConfiguration.getLogin())));
        http.addFilterBefore(distributedRateLimiterFilter, LogoutFilter.class);
        http.addFilterBefore(authenticationFilter, LogoutFilter.class);

        http.cors(c -> c.disable());
        http.csrf(c -> c.disable());

        return http.build();
    }

    /**
     *
     * @return
     */
    @Bean
    public ClientSettings clientSettings() {
        return ClientSettings.builder()
                .requireAuthorizationConsent(true)
                .requireProofKey(false)
                .build();
    }

    /**
     *
     * @param jwkSource
     * @return
     */
    @Bean
    public JwtEncoder jwtEncoder(JWKSource<SecurityContext> jwkSource) {
        return new NimbusJwtEncoder(jwkSource);
    }

    /**
     *
     * @param jwkSource
     * @return
     */
    @Bean
    public JwtDecoder jwtDecoder(JWKSource<SecurityContext> jwkSource) {
        return org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration.jwtDecoder(jwkSource);
    }

    /**
     *
     * @return
     */
    @Bean
    public PasswordEncoder passwordEncoder() {
        return NoOpPasswordEncoder.getInstance();
    }
}
