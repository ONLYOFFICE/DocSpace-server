package com.asc.registration.application.configuration;

import java.util.List;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 * The ApplicationConfiguration class represents the configuration properties for the application.
 */
@Getter
@Setter
@Configuration
@NoArgsConstructor
@ConfigurationProperties(prefix = "application")
public class ApplicationConfiguration {
  private List<ScopeConfiguration> scopes = List.of();

  /** The ScopeConfiguration class represents the configuration properties for a specific scope. */
  @Getter
  @Setter
  @AllArgsConstructor
  @NoArgsConstructor
  public static class ScopeConfiguration {
    private String name;
    private String group;
    private String type;
  }
}
