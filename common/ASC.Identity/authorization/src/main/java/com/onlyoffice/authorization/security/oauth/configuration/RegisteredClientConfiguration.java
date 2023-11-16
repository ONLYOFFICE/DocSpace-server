/**
 *
 */
package com.onlyoffice.authorization.security.oauth.configuration;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "spring.security.oauth2.registered.client")
public class RegisteredClientConfiguration {
    private int accessTokenMinutesTTL = 60;
    private int refreshTokenDaysTTL = 365;
    private int authorizationCodeMinutesTTL = 1;
}
