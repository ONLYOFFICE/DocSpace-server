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

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.registration.application.service.ConsentService;
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
  @SpringBootApplication(scanBasePackages = {"com.asc.registration", "com.asc.common"})
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
