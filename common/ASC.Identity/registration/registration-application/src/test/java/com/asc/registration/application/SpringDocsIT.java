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
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.registration.application.service.ConsentService;
import java.nio.file.Files;
import java.nio.file.Path;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.context.annotation.Import;
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
import org.testcontainers.junit.jupiter.Testcontainers;

@Testcontainers
@ActiveProfiles({"test", "server"})
@Import(RegistrationTestBeanConfiguration.class)
@EnabledIfSystemProperty(named = "RUN_DOCS_GENERATION", matches = "true")
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    classes = SpringDocsIT.TestApplication.class)
public class SpringDocsIT {
  @LocalServerPort int serverPort;

  static MySQLContainer<?> mysql = RegistrationTestContainers.mysql();
  static GenericContainer<?> redis = RegistrationTestContainers.redis();
  static RabbitMQContainer rabbitmq = RegistrationTestContainers.rabbitmq();

  static {
    mysql.start();
    rabbitmq.start();
    redis.start();
  }

  @DynamicPropertySource
  static void configureProperties(DynamicPropertyRegistry registry) {
    RegistrationTestContainers.configureMySql(registry, mysql);
    RegistrationTestContainers.configureRabbitMq(registry, rabbitmq);
    RegistrationTestContainers.configureRedis(registry, redis);
  }

  @EntityScan(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
  @SpringBootApplication(scanBasePackages = {"com.asc.registration", "com.asc.common"})
  @EnableJpaRepositories(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
  static class TestApplication {}

  @MockitoBean private ConsentService consentService;

  @MockitoBean
  private AuthorizationServiceGrpc.AuthorizationServiceBlockingStub authorizationServiceClient;

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
            .baseUrl("http://localhost:" + serverPort)
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
        resolveIdentityRoot().resolve("docs").resolve("identity-registration-openapi.json");
    Files.createDirectories(outputPath.getParent());
    Files.writeString(outputPath, response.getBody());
  }
}
