/**
 *
 */
package com.onlyoffice.authorization.security.configuration;

import com.onlyoffice.authorization.security.access.filters.ClientFilter;
import lombok.Data;
import lombok.SneakyThrows;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

/**
 *
 */
@Data
@Configuration
@EnableWebSecurity
@ConfigurationProperties("spring.security.oauth2.server")
public class ApplicationConfiguration {
    private final ClientFilter clientFilter;
    private final String login = "/oauth2/login";
    private final String consent = "/oauth2/consent";

    private String cipherSecret = "secret";
    private String issuer;

    @Bean
    @SneakyThrows
    SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) {
        return http
                .authorizeHttpRequests(authorizeRequests -> authorizeRequests.anyRequest().permitAll())
                .formLogin(form -> form
                                .loginPage(login)
                                .loginProcessingUrl(login)
                                .permitAll())
                .logout(l -> l.disable())
                .addFilterBefore(clientFilter, UsernamePasswordAuthenticationFilter.class)
                .build();
    }
}
