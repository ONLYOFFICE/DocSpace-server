/**
 *
 */
package com.onlyoffice.authorization.security.configuration;

import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import com.onlyoffice.authorization.security.crypto.jwks.JwksKeyPairGenerator;
import com.onlyoffice.authorization.security.oauth.filters.AnonymousReplacerAuthenticationFilter;
import com.onlyoffice.authorization.security.oauth.providers.DocspaceAuthenticationProvider;
import jakarta.servlet.RequestDispatcher;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.core.Authentication;
import org.springframework.security.crypto.password.NoOpPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.oauth2.jose.jws.SignatureAlgorithm;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtEncoder;
import org.springframework.security.oauth2.jwt.NimbusJwtEncoder;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configuration.OAuth2AuthorizationServerConfiguration;
import org.springframework.security.oauth2.server.authorization.config.annotation.web.configurers.OAuth2AuthorizationServerConfigurer;
import org.springframework.security.oauth2.server.authorization.settings.AuthorizationServerSettings;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.oauth2.server.authorization.token.JwtEncodingContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenCustomizer;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.AuthenticationFailureHandler;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.security.web.authentication.logout.LogoutFilter;
import org.springframework.security.web.util.matcher.AntPathRequestMatcher;

import java.security.NoSuchAlgorithmException;
import java.util.List;

/**
 *
 */
@Configuration
@RequiredArgsConstructor
public class AuthorizationServerConfiguration {
    private final String CONSENT_URL = "/oauth2/consent";
    @Autowired
    @Qualifier("ec")
    private JwksKeyPairGenerator generator;
    private final ApplicationConfiguration applicationConfiguration;
    private final DocspaceAuthenticationProvider authenticationProvider;
    private final AuthenticationSuccessHandler authenticationSuccessHandler;
    private final AuthenticationFailureHandler authenticationFailureHandler;
    private final AnonymousReplacerAuthenticationFilter authenticationFilter;

    @Bean
    @Order(Ordered.HIGHEST_PRECEDENCE)
    @SneakyThrows
    public SecurityFilterChain authorizationServerSecurityFilterChain(HttpSecurity http) {
        OAuth2AuthorizationServerConfiguration.applyDefaultSecurity(http);

        http.getConfigurer(OAuth2AuthorizationServerConfigurer.class)
                .oidc(Customizer.withDefaults())
                .authorizationEndpoint(e -> {
                    e.consentPage(CONSENT_URL);
                    e.authenticationProvider(authenticationProvider);
                    e.authorizationResponseHandler(authenticationSuccessHandler);
                    e.errorResponseHandler(authenticationFailureHandler);
                });

        http.exceptionHandling(e -> e.defaultAuthenticationEntryPointFor((request, response, authException) -> {
            RequestDispatcher dispatcher = request.getRequestDispatcher(applicationConfiguration.getLogin());
            dispatcher.forward(request, response);
        }, new AntPathRequestMatcher(applicationConfiguration.getLogin())));
        http.addFilterBefore(authenticationFilter, LogoutFilter.class);

        http.cors(c -> c.disable());
        http.csrf(c -> c.disable());

        return http.build();
    }

    @Bean
    public AuthorizationServerSettings authorizationServerSettings() {
        return AuthorizationServerSettings.builder()
                .issuer(applicationConfiguration.getIssuer())
                .build();
    }

    @Bean
    public ClientSettings clientSettings() {
        return ClientSettings.builder()
                .requireAuthorizationConsent(true)
                .requireProofKey(false)
                .build();
    }

    @Bean
    public JwtEncoder jwtEncoder(JWKSource<SecurityContext> jwkSource) {
        return new NimbusJwtEncoder(jwkSource);
    }

    @Bean
    public JwtDecoder jwtDecoder(JWKSource<SecurityContext> jwkSource) {
        return OAuth2AuthorizationServerConfiguration.jwtDecoder(jwkSource);
    }

    @Bean
    public JWKSource<SecurityContext> jwkSource() throws NoSuchAlgorithmException {
        JWKSet jwkSet = new JWKSet(generator.generateKey());
        return (jwkSelector, securityContext) -> jwkSelector.select(jwkSet);
    }

    @Bean
    public OAuth2TokenCustomizer<JwtEncodingContext> jwtCustomizer() {
        return context -> {
            Authentication principal = context.getPrincipal();
            var authority = principal.getAuthorities().stream().findFirst()
                    .orElse(null);
            if (principal.getDetails() != null)
                context.getClaims()
                        .subject(principal.getDetails().toString());
            if (authority != null)
                context.getClaims()
                        .issuer(String.format("%s/oauth2", authority.getAuthority()))
                        .audience(List.of(authority.getAuthority()));
            context
                    .getJwsHeader()
                    .algorithm(SignatureAlgorithm.ES256);
        };
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return NoOpPasswordEncoder.getInstance();
    }
}
