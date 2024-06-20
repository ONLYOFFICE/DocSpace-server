package com.asc.authorization.application.configuration.security;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/** Configuration properties for anonymous filter security settings. */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "security.auth")
public class AnonymousFilterSecurityConfigurationProperties {
  /** The name of the authentication cookie. Default value is "asc_auth_key". */
  private String authCookieName = "asc_auth_key";

  /** The parameter name for the client ID. Default value is "client_id". */
  private String clientIdParam = "client_id";

  /** The name of the redirect header. Default value is "X-Redirect-URI". */
  private String redirectHeader = "X-Redirect-URI";
}
