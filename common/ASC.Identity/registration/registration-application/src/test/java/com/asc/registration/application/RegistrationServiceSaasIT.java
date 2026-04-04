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

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.registration.application.service.ConsentService;
import net.devh.boot.grpc.client.autoconfigure.GrpcClientMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerSecurityAutoConfiguration;
import org.junit.jupiter.api.condition.EnabledIfSystemProperty;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.annotation.Import;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;
import org.testcontainers.containers.localstack.LocalStackContainer;
import org.testcontainers.junit.jupiter.Testcontainers;

@Testcontainers
@ActiveProfiles({"test", "saas"})
@Import(RegistrationTestBeanConfiguration.class)
@EnabledIfSystemProperty(named = "RUN_INTEGRATION_TESTS", matches = "true")
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    classes = RegistrationServiceSaasIT.TestApplication.class)
public class RegistrationServiceSaasIT extends AbstractRegistrationServiceIT {
  static MySQLContainer<?> mysql = RegistrationTestContainers.mysql();
  static GenericContainer<?> redis = RegistrationTestContainers.redis();
  static RabbitMQContainer rabbitmq = RegistrationTestContainers.rabbitmq();
  static LocalStackContainer localstack = RegistrationTestContainers.localstack();

  static {
    localstack.start();
    mysql.start();
    rabbitmq.start();
    redis.start();
    RegistrationTestContainers.createDynamoDbTable(localstack);
  }

  @DynamicPropertySource
  static void configureProperties(DynamicPropertyRegistry registry) {
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
}
