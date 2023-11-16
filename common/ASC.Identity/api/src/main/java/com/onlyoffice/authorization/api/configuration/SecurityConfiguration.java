/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import com.onlyoffice.authorization.api.security.filters.CheckAuthAdminCookieFilter;
import com.onlyoffice.authorization.api.security.filters.CheckAuthCookieFilter;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

/**
 *
 */
@Configuration
@EnableWebSecurity
@RequiredArgsConstructor
public class SecurityConfiguration {
    private final CheckAuthAdminCookieFilter adminCookieFilter;
    private final CheckAuthCookieFilter cookieFilter;
    @Bean
    SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) throws Exception {
        return http
                .authorizeHttpRequests(authorizeRequests -> authorizeRequests.anyRequest().permitAll())
                .addFilterAfter(cookieFilter, UsernamePasswordAuthenticationFilter.class)
                .addFilterAt(adminCookieFilter, UsernamePasswordAuthenticationFilter.class)
                .csrf(c -> c.disable())
                .cors(c -> c.disable())
                .build();
    }
}
