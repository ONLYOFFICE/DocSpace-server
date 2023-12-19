/**
 *
 */
package com.onlyoffice.authorization.api;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.transaction.annotation.EnableTransactionManagement;

/**
 *
 */
@EnableAsync
@EnableScheduling
@EnableFeignClients
@EnableTransactionManagement
@SpringBootApplication
public class ApplicationAPI {
	public static void main(String[] args) {
		SpringApplication.run(ApplicationAPI.class, args);
	}
}
