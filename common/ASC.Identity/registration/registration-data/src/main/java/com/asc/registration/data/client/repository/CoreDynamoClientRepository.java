// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.registration.data.client.repository;

import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.data.client.entity.ClientDynamoEntity;
import java.time.ZonedDateTime;
import java.util.*;
import org.springframework.beans.factory.BeanInitializationException;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.TableSchema;
import software.amazon.awssdk.enhanced.dynamodb.model.*;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;

/**
 * Repository implementation for managing client entities in DynamoDB.
 *
 * <p>This class provides CRUD operations and query methods for {@link ClientDynamoEntity} objects
 * using the AWS SDK's DynamoDB Enhanced Client. It supports pagination, filtering by tenant and
 * creator, and updates to specific client attributes.
 */
@Repository
@Profile(value = "saas")
public class CoreDynamoClientRepository implements DynamoClientRepository {
  private static final TableSchema<ClientDynamoEntity> CLIENT_SCHEMA =
      TableSchema.fromBean(ClientDynamoEntity.class);

  private final DynamoDbEnhancedClient dynamoDbEnhancedClient;
  private final DynamoDbTable<ClientDynamoEntity> clientTable;

  /**
   * Constructs a new instance of {@code CoreDynamoClientRepository} using the specified table name
   * and DynamoDB enhanced client.
   *
   * @param tableName the name of the DynamoDB table containing client entities, as configured by
   *     {@code spring.cloud.aws.dynamodb.tables.registeredClient}
   * @param dynamoDbEnhancedClient the enhanced DynamoDB client instance
   * @throws BeanInitializationException if the provided table name is null or blank
   */
  public CoreDynamoClientRepository(
      @Value("${spring.cloud.aws.dynamodb.tables.registeredClient}") String tableName,
      DynamoDbEnhancedClient dynamoDbEnhancedClient) {
    if (tableName == null || tableName.isBlank())
      throw new BeanInitializationException(
          "DynamoDB registered client table name is not provided");
    this.dynamoDbEnhancedClient = dynamoDbEnhancedClient;
    clientTable = dynamoDbEnhancedClient.table(tableName, CLIENT_SCHEMA);
  }

  /**
   * Persists a new client entity into DynamoDB.
   *
   * @param entity the {@link ClientDynamoEntity} to be saved
   */
  public void save(ClientDynamoEntity entity) {
    clientTable.putItem(entity);
  }

  /**
   * Updates an existing client entity in DynamoDB.
   *
   * @param entity the {@link ClientDynamoEntity} with updated attributes
   * @return the updated {@link ClientDynamoEntity}
   */
  public ClientDynamoEntity update(ClientDynamoEntity entity) {
    return clientTable.updateItem(entity);
  }

  /**
   * Retrieves a client entity by its unique client ID.
   *
   * @param clientId the unique identifier of the client
   * @return the corresponding {@link ClientDynamoEntity} if found, or {@code null} otherwise
   */
  public ClientDynamoEntity findById(String clientId) {
    return clientTable
        .query(QueryConditional.keyEqualTo(k -> k.partitionValue(clientId)))
        .items()
        .stream()
        .findFirst()
        .orElse(null);
  }

  /**
   * Retrieves a client entity by its client ID and visibility status.
   *
   * @param clientId the unique identifier of the client
   * @param accessible the desired visibility status (true for accessible, false otherwise)
   * @return an {@link Optional} containing the matching {@link ClientDynamoEntity} if found, or an
   *     empty {@link Optional} if not found
   */
  public Optional<ClientDynamoEntity> findByIdAndVisibility(String clientId, boolean accessible) {
    return clientTable
        .query(
            r -> r.queryConditional(QueryConditional.keyEqualTo(k -> k.partitionValue(clientId))))
        .items()
        .stream()
        .filter(client -> client.isAccessible() == accessible)
        .findFirst();
  }

  /**
   * Retrieves a paginated list of client entities for a specific tenant.
   *
   * <p>This method uses the "tenant-created-index" to query client entities by tenant ID.
   * Pagination is supported through the use of {@code nextClientId} and {@code nextCreatedOn} as
   * cursor parameters. Results are returned in descending order based on the creation timestamp.
   *
   * @param tenantId the tenant identifier
   * @param limit the maximum number of client entities to return
   * @param nextClientId the client ID serving as the pagination cursor, or {@code null} for the
   *     first page
   * @param nextCreatedOn the creation timestamp serving as the pagination cursor, or {@code null}
   *     for the first page
   * @return a list of {@link ClientDynamoEntity} objects matching the query
   */
  public List<ClientDynamoEntity> findAllByTenantId(
      long tenantId, int limit, String nextClientId, ZonedDateTime nextCreatedOn) {
    var index = clientTable.index("tenant-created-index");
    var queryConditional = QueryConditional.keyEqualTo(k -> k.partitionValue(tenantId));

    Map<String, AttributeValue> lastEvaluatedKey = null;
    var results = new ArrayList<ClientDynamoEntity>();

    if (nextClientId != null && !nextClientId.isBlank() && nextCreatedOn != null)
      lastEvaluatedKey =
          Map.of(
              "tenant_id", AttributeValue.builder().n(String.valueOf(tenantId)).build(),
              "client_id", AttributeValue.builder().s(nextClientId).build(),
              "created_on", AttributeValue.builder().s(nextCreatedOn.toString()).build());

    do {
      var requestBuilder =
          QueryEnhancedRequest.builder()
              .queryConditional(queryConditional)
              .scanIndexForward(false)
              .limit(20);

      if (lastEvaluatedKey != null) requestBuilder.exclusiveStartKey(lastEvaluatedKey);

      var request = requestBuilder.build();
      var queryResults = index.query(request);

      var page = queryResults.stream().findFirst();
      if (page.isPresent()) {
        results.addAll(page.get().items());
        lastEvaluatedKey = page.get().lastEvaluatedKey();
      } else lastEvaluatedKey = null;
    } while (lastEvaluatedKey != null && results.size() <= limit + 1);

    return results;
  }

  /**
   * Retrieves a paginated list of client entities created by a specific creator.
   *
   * <p>This method uses the "creator-created-index" to query client entities by creator ID.
   * Pagination is supported through the use of {@code nextClientId} and {@code nextCreatedOn} as
   * cursor parameters. Results are returned in descending order based on the creation timestamp.
   *
   * @param creatorId the identifier of the creator
   * @param limit the maximum number of client entities to return
   * @param nextClientId the client ID serving as the pagination cursor, or {@code null} for the
   *     first page
   * @param nextCreatedOn the creation timestamp serving as the pagination cursor, or {@code null}
   *     for the first page
   * @return a list of {@link ClientDynamoEntity} objects matching the query
   */
  public List<ClientDynamoEntity> findAllByCreatorId(
      String creatorId, int limit, String nextClientId, ZonedDateTime nextCreatedOn) {
    var index = clientTable.index("creator-created-index");
    var queryConditional = QueryConditional.keyEqualTo(k -> k.partitionValue(creatorId));

    Map<String, AttributeValue> lastEvaluatedKey = null;
    var results = new ArrayList<ClientDynamoEntity>();

    if (nextClientId != null && !nextClientId.isBlank() && nextCreatedOn != null)
      lastEvaluatedKey =
          Map.of(
              "creator_id", AttributeValue.builder().s(creatorId).build(),
              "client_id", AttributeValue.builder().s(nextClientId).build(),
              "created_on", AttributeValue.builder().s(nextCreatedOn.toString()).build());

    do {
      var requestBuilder =
          QueryEnhancedRequest.builder()
              .queryConditional(queryConditional)
              .scanIndexForward(false)
              .limit(20);

      if (lastEvaluatedKey != null) requestBuilder.exclusiveStartKey(lastEvaluatedKey);

      var request = requestBuilder.build();
      var queryResults = index.query(request);

      var page = queryResults.stream().findFirst();
      if (page.isPresent()) {
        results.addAll(page.get().items());
        lastEvaluatedKey = page.get().lastEvaluatedKey();
      } else lastEvaluatedKey = null;
    } while (lastEvaluatedKey != null && results.size() <= limit + 1);

    return results;
  }

  /**
   * Retrieves a client entity by its client ID and tenant ID.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @return an {@link Optional} containing the matching {@link ClientDynamoEntity} if found, or an
   *     empty {@link Optional} if not found
   */
  public Optional<ClientDynamoEntity> findByClientIdAndTenantId(String clientId, long tenantId) {
    return clientTable
        .query(QueryConditional.keyEqualTo(k -> k.partitionValue(clientId)))
        .items()
        .stream()
        .filter(c -> c.getTenantId() == tenantId)
        .findFirst();
  }

  /**
   * Retrieves all client entities with any of the specified client IDs.
   *
   * @param clientIds a list of client IDs to search for; if {@code null} or empty, an empty list is
   *     returned
   * @return a list of matching {@link ClientDynamoEntity} objects
   */
  public List<ClientDynamoEntity> findAllByClientIds(List<String> clientIds) {
    if (clientIds == null || clientIds.isEmpty()) return Collections.emptyList();

    var readBatchBuilder =
        ReadBatch.builder(ClientDynamoEntity.class).mappedTableResource(clientTable);

    clientIds.forEach(
        clientId -> readBatchBuilder.addGetItem(Key.builder().partitionValue(clientId).build()));

    var batchGetItemRequest =
        BatchGetItemEnhancedRequest.builder().readBatches(readBatchBuilder.build()).build();

    var resultPages = dynamoDbEnhancedClient.batchGetItem(batchGetItemRequest);

    var results = new ArrayList<ClientDynamoEntity>();
    resultPages.resultsForTable(clientTable).forEach(results::add);

    return results;
  }

  /**
   * Deletes a client entity by its client ID and tenant ID.
   *
   * @param clientId the unique identifier of the client to delete
   * @param tenantId the tenant identifier
   * @return the deleted {@link ClientDynamoEntity} if deletion was successful, or {@code null}
   *     otherwise
   */
  public ClientDynamoEntity deleteByIdAndTenantId(String clientId, long tenantId) {
    var keyEntity = new ClientDynamoEntity();
    keyEntity.setClientId(clientId);
    keyEntity.setTenantId(tenantId);
    return clientTable.deleteItem(keyEntity);
  }

  /**
   * Updates the client secret for a specific client entity.
   *
   * <p>This method retrieves the existing client entity, updates its secret and modification
   * timestamp, and persists the changes.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param secret the new client secret to set
   * @param modifiedOn the timestamp when the update is performed
   * @return the updated {@link ClientDynamoEntity}
   */
  public ClientDynamoEntity updateClientSecret(
      String clientId, long tenantId, String secret, ZonedDateTime modifiedOn) {
    var updatedClient = buildUpdatedClient(clientId, tenantId, "clientSecret", secret, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Updates the visibility status for a specific client entity.
   *
   * <p>This method retrieves the existing client entity, updates its accessibility flag and
   * modification timestamp, and persists the changes.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param accessible the new visibility status (true for accessible, false otherwise)
   * @param modifiedOn the timestamp when the update is performed
   * @return the updated {@link ClientDynamoEntity}
   */
  public ClientDynamoEntity updateVisibility(
      String clientId, long tenantId, boolean accessible, ZonedDateTime modifiedOn) {
    var updatedClient =
        buildUpdatedClient(clientId, tenantId, "accessible", accessible, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Updates the activation status for a specific client entity.
   *
   * <p>This method retrieves the existing client entity, updates its enabled flag and modification
   * timestamp, and persists the changes.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param enabled the new activation status (true if enabled, false otherwise)
   * @param modifiedOn the timestamp when the update is performed
   * @return the updated {@link ClientDynamoEntity}
   */
  public ClientDynamoEntity updateActivation(
      String clientId, long tenantId, boolean enabled, ZonedDateTime modifiedOn) {
    var updatedClient = buildUpdatedClient(clientId, tenantId, "enabled", enabled, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Constructs an updated client entity with modified attributes.
   *
   * <p>This helper method retrieves the existing client entity by client ID and tenant ID, updates
   * the specified attribute with the new value, and sets the modification timestamp.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param attributeName the name of the attribute to update ("clientSecret", "accessible", or
   *     "enabled")
   * @param value the new value to set for the specified attribute
   * @param modifiedOn the timestamp when the update is performed
   * @return the updated {@link ClientDynamoEntity}
   * @throws ClientNotFoundException if no client entity is found for the given client ID and tenant
   *     ID
   */
  private ClientDynamoEntity buildUpdatedClient(
      String clientId,
      long tenantId,
      String attributeName,
      Object value,
      ZonedDateTime modifiedOn) {
    var existingClient =
        findByClientIdAndTenantId(clientId, tenantId)
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with id %s for tenant %d was not found", clientId, tenantId)));

    switch (attributeName) {
      case "clientSecret" -> existingClient.setClientSecret((String) value);
      case "accessible" -> existingClient.setAccessible((boolean) value);
      case "enabled" -> existingClient.setEnabled((boolean) value);
    }

    existingClient.setModifiedOn(modifiedOn.toString());
    return existingClient;
  }
}
