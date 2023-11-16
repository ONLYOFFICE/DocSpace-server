/**
 *
 */
package com.onlyoffice.authorization.api;

import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;

/**
 *
 */
public class ContainerBase {
    private static MySQLContainer sqlContainer;
    private static RabbitMQContainer rabbitContainer;

    static {
        sqlContainer = new MySQLContainer("mysql:latest")
                .withDatabaseName("docspace")
                .withUsername("root")
                .withPassword("root");
        rabbitContainer = new RabbitMQContainer("rabbitmq:latest");
        sqlContainer.start();
        rabbitContainer.start();
    }

    @DynamicPropertySource
    public static void containersProperties(DynamicPropertyRegistry registry) {
        registry.add("server.port", () -> 12345);
        registry.add("server.socket.port", () -> 11223);
        registry.add("spring.datasource.username", sqlContainer::getUsername);
        registry.add("spring.datasource.password", sqlContainer::getPassword);
        registry.add("spring.datasource.url", sqlContainer::getJdbcUrl);
        registry.add("spring.flyway.enabled", () -> false);
        registry.add("spring.flyway.url", sqlContainer::getJdbcUrl);
        registry.add("spring.flyway.user", sqlContainer::getUsername);
        registry.add("spring.flyway.password", sqlContainer::getPassword);
        registry.add("spring.jpa.hibernate.ddl-auto", () -> "update");
        registry.add("spring.rabbitmq.host", rabbitContainer::getHost);
        registry.add("spring.rabbitmq.port", rabbitContainer::getAmqpPort);
    }
}
