/**
 *
 */
package com.onlyoffice.authorization.api;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.context.event.EventListener;
import org.springframework.transaction.annotation.EnableTransactionManagement;

import java.util.TimeZone;

/**
 *
 */
@EnableFeignClients
@EnableTransactionManagement
@SpringBootApplication
public class ApplicationAPI {
	public static void main(String[] args) {
		SpringApplication.run(ApplicationAPI.class, args);
	}

	@EventListener(ContextRefreshedEvent.class)
	public void contextRefreshedEvent() {
		TimeZone.setDefault(TimeZone.getTimeZone("UTC"));
	}
}
