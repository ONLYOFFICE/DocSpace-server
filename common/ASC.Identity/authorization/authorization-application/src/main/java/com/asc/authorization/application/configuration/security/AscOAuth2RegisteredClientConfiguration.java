package com.asc.authorization.application.configuration.security;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/** Configuration properties for OAuth2 registered client settings. */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "spring.security.oauth2.registered-client")
public class AscOAuth2RegisteredClientConfiguration {
  /** Time-to-live (TTL) for access tokens, in minutes. Default value is 60 minutes. */
  private int accessTokenMinutesTTL = 60;

  /** Time-to-live (TTL) for refresh tokens, in days. Default value is 365 days. */
  private int refreshTokenDaysTTL = 365;

  /** Time-to-live (TTL) for authorization codes, in minutes. Default value is 1 minute. */
  private int authorizationCodeMinutesTTL = 1;
}
