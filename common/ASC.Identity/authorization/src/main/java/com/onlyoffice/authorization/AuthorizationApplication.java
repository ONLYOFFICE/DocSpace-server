/**
 *
 */
package com.onlyoffice.authorization;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cloud.openfeign.EnableFeignClients;
import org.springframework.context.annotation.EnableAspectJAutoProxy;

/**
 *
 */
@EnableCaching
@EnableFeignClients
@SpringBootApplication
@EnableAspectJAutoProxy
public class AuthorizationApplication {
	public static void main(String[] args) {
		SpringApplication.run(AuthorizationApplication.class, args);
	}
}
