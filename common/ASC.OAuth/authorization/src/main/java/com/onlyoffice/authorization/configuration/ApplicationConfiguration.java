package com.onlyoffice.authorization.configuration;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
@ConfigurationProperties(prefix = "application.server")
@EnableWebSecurity
@Getter
@Setter
@RequiredArgsConstructor
public class ApplicationConfiguration {
    private String url;
    private String login = "/oauth2/login";
    private String logout = "/logout";

    private int maxConnections = 100;
    private int defaultPerRoute = 20;
    private int connectionRequestTimeout = 3; // request's connection timeout in seconds
    private int responseTimeout = 3; // timeout to get response in seconds

    @Bean
    SecurityFilterChain configureSecurityFilterChain(HttpSecurity http) throws Exception {
        return http
                .authorizeHttpRequests(authorizeRequests -> authorizeRequests.anyRequest().permitAll())
                .formLogin(
                        form -> form
                                .loginPage(this.login)
                                .loginProcessingUrl(this.login)
                                .permitAll()
                )
                .logout(
                        logout -> logout
                                .logoutUrl(this.logout)
                                .clearAuthentication(true)
                                .invalidateHttpSession(true)
                )
                .build();
    }
}
