package com.asc.registration.container;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.domain.EntityScan;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;

@EnableCaching
@EntityScan(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
@EnableJpaRepositories(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
@SpringBootApplication(scanBasePackages = {"com.asc.registration", "com.asc.common"})
@EnableFeignClients(basePackages = "com.asc.common.application.client")
public class RegistrationServiceApplication {
  public static void main(String[] args) {
    SpringApplication.run(RegistrationServiceApplication.class, args);
  }
}
