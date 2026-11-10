// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.registration.application;

import static org.assertj.core.api.Assertions.assertThat;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.registration.application.service.ConsentService;
import com.asc.registration.application.transfer.CreateClientRequest;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.ArrayList;
import java.util.Base64;
import java.util.HashSet;
import java.util.Random;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.IntStream;
import net.devh.boot.grpc.client.autoconfigure.GrpcClientMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerSecurityAutoConfiguration;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.annotation.Import;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.http.HttpHeaders;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;
import org.testcontainers.containers.localstack.LocalStackContainer;
import org.testcontainers.junit.jupiter.Container;
import org.testcontainers.junit.jupiter.Testcontainers;
import org.testcontainers.lifecycle.Startables;

@Testcontainers
@ActiveProfiles({"test", "saas"})
@Import(RegistrationTestBeanConfiguration.class)
@EnabledIfSystemProperty(named = "RUN_INTEGRATION_TESTS", matches = "true")
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    classes = RegistrationServiceSaasIT.TestApplication.class)
public class RegistrationServiceSaasIT extends AbstractRegistrationServiceIT {
  @Container static final MySQLContainer<?> mysql = RegistrationTestContainers.mysql();
  @Container static final GenericContainer<?> redis = RegistrationTestContainers.redis();
  @Container static final RabbitMQContainer rabbitmq = RegistrationTestContainers.rabbitmq();
  @Container static final LocalStackContainer localstack = RegistrationTestContainers.localstack();

  private static void startContainers() {
    Startables.deepStart(mysql, redis, rabbitmq, localstack).join();
  }

  @BeforeAll
  static void initializeDynamoDb() {
    startContainers();
    RegistrationTestContainers.createDynamoDbTable(localstack);
  }

  @DynamicPropertySource
  static void configureProperties(DynamicPropertyRegistry registry) {
    startContainers();
    RegistrationTestContainers.configureMySql(registry, mysql);
    RegistrationTestContainers.configureRabbitMq(registry, rabbitmq);
    RegistrationTestContainers.configureRedis(registry, redis);
    RegistrationTestContainers.configureDynamoDb(registry, localstack);
  }

  @EntityScan(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
  @SpringBootApplication(
      scanBasePackages = {"com.asc.registration", "com.asc.common"},
      exclude = {
        GrpcServerSecurityAutoConfiguration.class,
        GrpcServerMetricAutoConfiguration.class,
        GrpcClientMetricAutoConfiguration.class
      })
  @EnableJpaRepositories(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
  static class TestApplication {}

  @MockitoBean private ConsentService consentService;

  @MockitoBean
  private AuthorizationServiceGrpc.AuthorizationServiceBlockingStub authorizationServiceClient;

  @Override
  protected ConsentService getConsentService() {
    return consentService;
  }

  @Override
  protected AuthorizationServiceGrpc.AuthorizationServiceBlockingStub
      getAuthorizationServiceClient() {
    return authorizationServiceClient;
  }

  private static CreateClientRequest maximumSizeClientRequest(String name, Random random) {
    var logo = new byte[25600];
    random.nextBytes(logo);
    return CreateClientRequest.builder()
        .name(name + "n".repeat(256 - name.length()))
        .description("d".repeat(255))
        .logo("data:image/png;base64," + Base64.getEncoder().encodeToString(logo))
        .allowPkce(false)
        .isPublic(false)
        .websiteUrl("https://example.com")
        .termsUrl("https://example.com/terms")
        .policyUrl("https://example.com/policy")
        .redirectUris(
            IntStream.range(0, 12)
                .mapToObj(i -> "https://example.com/redirect/%02d/%s".formatted(i, "p".repeat(200)))
                .collect(Collectors.toSet()))
        .allowedOrigins(
            IntStream.range(0, 12)
                .mapToObj(i -> "https://o%02d%s.example.com".formatted(i, "q".repeat(50)))
                .collect(Collectors.toSet()))
        .logoutRedirectUri("https://example.com/logout")
        .scopes(Set.of("files:read"))
        .build();
  }

  private JsonNode listClients(int limit, JsonNode cursor, String cookie) throws Exception {
    var body =
        restClient()
            .get()
            .uri(
                builder -> {
                  builder.path(WEB_API + "/clients").queryParam("limit", limit);
                  if (cursor != null)
                    builder
                        .queryParam("last_client_id", cursor.get("last_client_id").asText())
                        .queryParam("last_created_on", cursor.get("last_created_on").asText());
                  return builder.build();
                })
            .header(HttpHeaders.COOKIE, cookie)
            .retrieve()
            .body(String.class);
    return new ObjectMapper().readTree(body);
  }

  @Test
  void givenTenantAtClientCapWithMaximumSizeClients_whenListing_thenEveryClientIsReturned()
      throws Exception {
    var tenantId = 2L;
    var random = new Random(42);
    var created = new ArrayList<String>();
    for (var user = 0; user < 3; user++) {
      var cookie = signatureCookie(randomUserId(), tenantId, false, false, true);
      for (var i = 0; i < 10; i++) {
        var response =
            createClient(maximumSizeClientRequest("list-" + user + "-" + i, random), cookie);

        assertCreated(response);

        created.add(extractJsonField(response.getBody(), "client_id"));
      }
    }

    var adminCookie = signatureCookie(randomUserId(), tenantId, true, false, false);
    var page = listClients(50, null, adminCookie);

    assertThat(page.get("data")).hasSize(30);
    assertThat(page.path("last_client_id").isMissingNode() || page.get("last_client_id").isNull())
        .isTrue();

    var seen = new HashSet<String>();
    JsonNode cursor = null;

    for (var pages = 0; pages < 5; pages++) {
      var next = listClients(10, cursor, adminCookie);
      next.get("data").forEach(client -> seen.add(client.get("client_id").asText()));
      if (next.path("last_client_id").isMissingNode() || next.get("last_client_id").isNull()) break;
      cursor = next;
    }

    assertThat(seen).containsExactlyInAnyOrderElementsOf(created);
    assertOk(deleteTenantClients(adminCookie));
  }
}
