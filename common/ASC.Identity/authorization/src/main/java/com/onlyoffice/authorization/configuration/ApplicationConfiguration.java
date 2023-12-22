package com.onlyoffice.authorization.configuration;

import com.onlyoffice.authorization.web.server.utilities.HttpUtils;
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
    private InstanceConfiguration instance = new InstanceConfiguration();
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

    /**
     *
     */
    @Getter
    @Setter
    @AllArgsConstructor
    public class InstanceConfiguration {
        private String issuer;
        public InstanceConfiguration() {
            try {
                issuer = HttpUtils.getIpAddress();
            } catch (Exception e) {}
        }
    }
}
