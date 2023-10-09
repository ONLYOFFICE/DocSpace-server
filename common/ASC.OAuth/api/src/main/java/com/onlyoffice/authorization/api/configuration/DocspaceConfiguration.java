package com.onlyoffice.authorization.api.configuration;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Configuration
@ConfigurationProperties(prefix = "docspace.server")
@Getter
@Setter
public class DocspaceConfiguration {
    private String url;
}

