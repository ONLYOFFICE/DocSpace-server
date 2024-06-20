package com.asc.authorization.container;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.domain.EntityScan;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

/**
 * Main class for the Authorization Service application.
 *
 * <p>This class is responsible for bootstrapping the Spring Boot application. It configures entity
 * scanning, JPA repositories, Feign clients, and the base packages to be scanned.
 */
@EnableCaching
@EntityScan(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
@EnableJpaRepositories(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
@SpringBootApplication(scanBasePackages = {"com.asc.authorization", "com.asc.common"})
@EnableFeignClients(basePackages = "com.asc.common.application.client")
public class AuthorizationServiceApplication {

  /**
   * The main method serves as the entry point for the Spring Boot application.
   *
   * @param args command line arguments passed to the application
   */
  public static void main(String[] args) {
    SpringApplication.run(AuthorizationServiceApplication.class, args);
  }
}
