/**
 *
 */
package com.asc.authorization.api.configuration;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

import java.util.List;

/**
 *
 */
@Getter
@Setter
@Configuration
@NoArgsConstructor
@ConfigurationProperties(prefix = "application")
public class ApplicationConfiguration {
    private SecurityConfiguration security = new SecurityConfiguration();
    private List<ScopeConfiguration> scopes = List.of();

    @Getter
    @Setter
    @AllArgsConstructor
    @NoArgsConstructor
    public class SecurityConfiguration {
        private String cipherSecret = "secret";
    }

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
