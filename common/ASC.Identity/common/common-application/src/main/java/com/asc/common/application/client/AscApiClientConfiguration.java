package com.asc.common.application.client;

import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.cloud.client.loadbalancer.LoadBalanced;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestTemplate;

/**
 * Configuration class for the Asc API client.
 *
 * <p>This class provides a Spring-managed {@link RestTemplate} bean that is configured with load
 * balancing capabilities.
 */
@Configuration
public class AscApiClientConfiguration {

  /**
   * Creates a new {@link RestTemplate} bean with load balancing capabilities.
   *
   * @return a new {@link RestTemplate} instance
   */
  @Bean
  @LoadBalanced
  @ConditionalOnMissingBean(RestTemplate.class)
  public RestTemplate restTemplate() {
    return new RestTemplate();
  }
}
