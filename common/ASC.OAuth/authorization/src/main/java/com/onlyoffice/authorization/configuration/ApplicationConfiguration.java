package com.onlyoffice.authorization.configuration;

import com.onlyoffice.authorization.extensions.filters.CookieCsrfFilter;
import com.onlyoffice.authorization.extensions.filters.SimpleCORSFilter;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import org.apache.hc.client5.http.classic.HttpClient;
import org.apache.hc.client5.http.config.RequestConfig;
import org.apache.hc.client5.http.impl.classic.HttpClientBuilder;
import org.apache.hc.client5.http.impl.io.PoolingHttpClientConnectionManager;
import org.apache.hc.core5.util.Timeout;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.HttpComponentsClientHttpRequestFactory;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.www.BasicAuthenticationFilter;
import org.springframework.security.web.csrf.CookieCsrfTokenRepository;
import org.springframework.security.web.csrf.CsrfTokenRequestAttributeHandler;
import org.springframework.web.client.RestTemplate;

import java.time.Duration;

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
    public RestTemplate restTemplate() {
        PoolingHttpClientConnectionManager connectionManager = new PoolingHttpClientConnectionManager();
        connectionManager.setMaxTotal(maxConnections);
        connectionManager.setDefaultMaxPerRoute(defaultPerRoute);

        RequestConfig requestConfig = RequestConfig
                .custom()
                .setConnectionRequestTimeout(Timeout.of(Duration.ofSeconds(connectionRequestTimeout)))
                .setResponseTimeout(Timeout.of(Duration.ofSeconds(responseTimeout)))
                .build();

        HttpClient httpClient = HttpClientBuilder.create()
                .setConnectionManager(connectionManager)
                .setDefaultRequestConfig(requestConfig).build();

        return new RestTemplate(new HttpComponentsClientHttpRequestFactory(httpClient));
    }

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
                                .deleteCookies("JSESSIONID")
                )

                .build();
    }
}
