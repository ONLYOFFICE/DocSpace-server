/**
 *
 */
package com.asc.authorization.api;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.security.servlet.UserDetailsServiceAutoConfiguration;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.transaction.annotation.EnableTransactionManagement;

/**
 *
 */
@EnableAsync
@EnableFeignClients
@EnableTransactionManagement
@SpringBootApplication(exclude = {UserDetailsServiceAutoConfiguration.class})
public class ApplicationAPI {
	public static void main(String[] args) {
		SpringApplication.run(ApplicationAPI.class, args);
	}
}
