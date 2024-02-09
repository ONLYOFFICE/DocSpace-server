package com.asc.authorization.configuration;

import com.asc.authorization.web.server.utilities.HttpUtils;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@Configuration
@NoArgsConstructor
@ConfigurationProperties(prefix = "application")
public class ApplicationConfiguration {
    private SecurityCipherConfiguration security = new SecurityCipherConfiguration();

    /**
     *
     */
    @Getter
    @Setter
    @NoArgsConstructor
    @AllArgsConstructor
    public class SecurityCipherConfiguration {
        private String cipherSecret = "secret";
    }
}
