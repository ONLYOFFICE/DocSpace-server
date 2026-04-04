// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.authorization.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.authorization.application.security.oauth.service.GrpcRegisteredClientService;
import com.asc.authorization.application.security.oauth.service.RegisteredClientService;
import com.asc.common.utilities.crypto.HashingService;
import java.nio.file.Files;
import java.nio.file.Path;
import net.devh.boot.grpc.client.autoconfigure.GrpcClientMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerSecurityAutoConfiguration;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.http.HttpStatusCode;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.web.client.RestClient;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;
import org.testcontainers.junit.jupiter.Container;
import org.testcontainers.junit.jupiter.Testcontainers;

@Testcontainers
@ActiveProfiles("test")
@EnabledIfSystemProperty(named = "RUN_DOCS_GENERATION", matches = "true")
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    classes = SpringDocsIT.TestApplication.class)
public class SpringDocsIT {
  @LocalServerPort int randomServerPort;

  @EntityScan(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
  @SpringBootApplication(
      scanBasePackages = {"com.asc.authorization", "com.asc.common"},
      exclude = {
        GrpcServerSecurityAutoConfiguration.class,
        GrpcServerMetricAutoConfiguration.class,
        GrpcClientMetricAutoConfiguration.class
      })
  @EnableJpaRepositories(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
  static class TestApplication {}

  @Container
  static MySQLContainer<?> mysql = new MySQLContainer<>("mysql:8.0").withInitScript("init.sql");

  @Container static RabbitMQContainer rabbitmq = new RabbitMQContainer("rabbitmq:3.11-management");

  @Container
  static GenericContainer<?> redis = new GenericContainer<>("redis:7.0").withExposedPorts(6379);

  @MockitoBean private RegisteredClientService registeredClientService;
  @MockitoBean private GrpcRegisteredClientService grpcRegisteredClientService;
  @MockitoBean private HashingService hashingService;

  @DynamicPropertySource
  static void configureTestContainers(DynamicPropertyRegistry registry) {
    registry.add("spring.datasource.url", mysql::getJdbcUrl);
    registry.add("spring.datasource.username", mysql::getUsername);
    registry.add("spring.datasource.password", mysql::getPassword);
    registry.add("spring.datasource.driver-class-name", () -> "com.mysql.cj.jdbc.Driver");

    registry.add("spring.rabbitmq.host", rabbitmq::getHost);
    registry.add("spring.rabbitmq.port", rabbitmq::getAmqpPort);
    registry.add("spring.rabbitmq.username", rabbitmq::getAdminUsername);
    registry.add("spring.rabbitmq.password", rabbitmq::getAdminPassword);

    registry.add("spring.data.redis.host", redis::getHost);
    registry.add("spring.data.redis.port", () -> redis.getMappedPort(6379));
  }

  private static Path resolveIdentityRoot() {
    var current = Path.of(System.getProperty("user.dir")).toAbsolutePath();

    while (current != null && !"ASC.Identity".equals(current.getFileName().toString())) {
      current = current.getParent();
    }

    if (current == null) {
      throw new IllegalStateException("Could not resolve ASC.Identity root folder.");
    }

    return current;
  }

  @Test
  void shouldGenerateAndSaveOpenApiDocument() throws Exception {
    var client =
        RestClient.builder()
            .baseUrl("http://localhost:" + randomServerPort)
            .defaultStatusHandler(HttpStatusCode::isError, (req, res) -> {})
            .build();

    var response = client.get().uri("/docs").retrieve().toEntity(String.class);
    if (!response.getStatusCode().is2xxSuccessful()) {
      response = client.get().uri("/v3/api-docs").retrieve().toEntity(String.class);
    }

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
    assertThat(response.getBody()).isNotBlank();
    assertThat(response.getBody()).contains("\"openapi\"");

    var outputPath =
        resolveIdentityRoot().resolve("docs").resolve("identity-authorization-openapi.json");
    Files.createDirectories(outputPath.getParent());
    Files.writeString(outputPath, response.getBody());
  }
}
