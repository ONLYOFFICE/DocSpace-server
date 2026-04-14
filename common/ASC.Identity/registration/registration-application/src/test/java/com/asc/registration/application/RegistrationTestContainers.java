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

import static org.testcontainers.containers.localstack.LocalStackContainer.Service.DYNAMODB;

import org.springframework.test.context.DynamicPropertyRegistry;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MySQLContainer;
import org.testcontainers.containers.RabbitMQContainer;
import org.testcontainers.containers.localstack.LocalStackContainer;
import org.testcontainers.utility.DockerImageName;
import software.amazon.awssdk.auth.credentials.AwsBasicCredentials;
import software.amazon.awssdk.auth.credentials.StaticCredentialsProvider;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.services.dynamodb.model.*;

public final class RegistrationTestContainers {
  public static final String DYNAMODB_TABLE_NAME = "RegisteredClient";
  private static final int REDIS_PORT = 6379;

  private RegistrationTestContainers() {}

  public static MySQLContainer<?> mysql() {
    return new MySQLContainer<>("mysql:8.0").withInitScript("init.sql");
  }

  public static RabbitMQContainer rabbitmq() {
    return new RabbitMQContainer("rabbitmq:3.11-management");
  }

  public static GenericContainer<?> redis() {
    return new GenericContainer<>("redis:7.0").withExposedPorts(REDIS_PORT);
  }

  public static LocalStackContainer localstack() {
    return new LocalStackContainer(DockerImageName.parse("localstack/localstack:3.0"))
        .withServices(DYNAMODB);
  }

  public static void configureMySql(DynamicPropertyRegistry registry, MySQLContainer<?> mysql) {
    registry.add("spring.datasource.url", mysql::getJdbcUrl);
    registry.add("spring.datasource.username", mysql::getUsername);
    registry.add("spring.datasource.password", mysql::getPassword);
    registry.add("spring.datasource.driver-class-name", () -> "com.mysql.cj.jdbc.Driver");
  }

  public static void configureRabbitMq(
      DynamicPropertyRegistry registry, RabbitMQContainer rabbitmq) {
    registry.add("spring.rabbitmq.host", rabbitmq::getHost);
    registry.add("spring.rabbitmq.port", rabbitmq::getAmqpPort);
    registry.add("spring.rabbitmq.username", rabbitmq::getAdminUsername);
    registry.add("spring.rabbitmq.password", rabbitmq::getAdminPassword);
  }

  public static void configureRedis(DynamicPropertyRegistry registry, GenericContainer<?> redis) {
    registry.add("spring.data.redis.host", redis::getHost);
    registry.add("spring.data.redis.port", () -> redis.getMappedPort(REDIS_PORT));

    registry.add("client.cache.redis.enabled", () -> "true");
    registry.add("client.cache.redis.host", redis::getHost);
    registry.add("client.cache.redis.port", () -> redis.getMappedPort(REDIS_PORT));

    registry.add("bucket4j.enabled", () -> "true");
    registry.add("bucket4j.redis.host", redis::getHost);
    registry.add("bucket4j.redis.port", () -> redis.getMappedPort(REDIS_PORT));
  }

  public static void configureDynamoDb(
      DynamicPropertyRegistry registry, LocalStackContainer localstack) {
    registry.add("spring.cloud.aws.dynamodb.enabled", () -> "true");
    registry.add(
        "spring.cloud.aws.dynamodb.endpoint",
        () -> localstack.getEndpointOverride(DYNAMODB).toString());
    registry.add("spring.cloud.aws.dynamodb.tables.registeredClient", () -> DYNAMODB_TABLE_NAME);
    registry.add("spring.cloud.aws.credentials.access-key", localstack::getAccessKey);
    registry.add("spring.cloud.aws.credentials.secret-key", localstack::getSecretKey);
    registry.add("spring.cloud.aws.region.static", localstack::getRegion);
    registry.add("spring.cloud.aws.region.auto", () -> "false");
  }

  public static void createDynamoDbTable(LocalStackContainer localstack) {
    try (var client =
        DynamoDbClient.builder()
            .endpointOverride(localstack.getEndpointOverride(DYNAMODB))
            .credentialsProvider(
                StaticCredentialsProvider.create(
                    AwsBasicCredentials.create(
                        localstack.getAccessKey(), localstack.getSecretKey())))
            .region(Region.of(localstack.getRegion()))
            .build()) {

      client.createTable(
          CreateTableRequest.builder()
              .tableName(DYNAMODB_TABLE_NAME)
              .keySchema(
                  KeySchemaElement.builder()
                      .attributeName("client_id")
                      .keyType(KeyType.HASH)
                      .build())
              .attributeDefinitions(
                  AttributeDefinition.builder()
                      .attributeName("client_id")
                      .attributeType(ScalarAttributeType.S)
                      .build(),
                  AttributeDefinition.builder()
                      .attributeName("tenant_id")
                      .attributeType(ScalarAttributeType.N)
                      .build(),
                  AttributeDefinition.builder()
                      .attributeName("created_by")
                      .attributeType(ScalarAttributeType.S)
                      .build(),
                  AttributeDefinition.builder()
                      .attributeName("created_on")
                      .attributeType(ScalarAttributeType.S)
                      .build())
              .globalSecondaryIndexes(
                  GlobalSecondaryIndex.builder()
                      .indexName("tenant-created-index")
                      .keySchema(
                          KeySchemaElement.builder()
                              .attributeName("tenant_id")
                              .keyType(KeyType.HASH)
                              .build(),
                          KeySchemaElement.builder()
                              .attributeName("created_on")
                              .keyType(KeyType.RANGE)
                              .build())
                      .projection(Projection.builder().projectionType(ProjectionType.ALL).build())
                      .build(),
                  GlobalSecondaryIndex.builder()
                      .indexName("creator-created-index")
                      .keySchema(
                          KeySchemaElement.builder()
                              .attributeName("created_by")
                              .keyType(KeyType.HASH)
                              .build(),
                          KeySchemaElement.builder()
                              .attributeName("created_on")
                              .keyType(KeyType.RANGE)
                              .build())
                      .projection(Projection.builder().projectionType(ProjectionType.ALL).build())
                      .build())
              .billingMode(BillingMode.PAY_PER_REQUEST)
              .build());

      client
          .waiter()
          .waitUntilTableExists(
              DescribeTableRequest.builder().tableName(DYNAMODB_TABLE_NAME).build());
    }
  }
}
