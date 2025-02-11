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
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.TableSchema;
import software.amazon.awssdk.enhanced.dynamodb.model.*;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;

/**
 * Repository implementation for managing clients stored in DynamoDB.
 *
 * <p>Provides CRUD operations for client entities, including query methods for retrieving clients
 * by various attributes and handling updates to client properties.
 */
@Repository
@Profile(value = "saas")
public class CoreDynamoClientRepository implements DynamoClientRepository {
  private static final TableSchema<ClientDynamoEntity> CLIENT_SCHEMA =
      TableSchema.fromBean(ClientDynamoEntity.class);

  private final DynamoDbEnhancedClient dynamoDbEnhancedClient;
  private final DynamoDbTable<ClientDynamoEntity> clientTable;

  /**
   * Constructs a new instance of the repository with the given DynamoDB enhanced client.
   *
   * @param dynamoDbEnhancedClient the DynamoDB enhanced client.
   */
  public CoreDynamoClientRepository(DynamoDbEnhancedClient dynamoDbEnhancedClient) {
    this.dynamoDbEnhancedClient = dynamoDbEnhancedClient;
    clientTable = dynamoDbEnhancedClient.table("RegisteredClient", CLIENT_SCHEMA);
  }

  /**
   * Saves a new client entity in DynamoDB.
   *
   * @param entity the client entity to save.
   */
  public void save(ClientDynamoEntity entity) {
    clientTable.putItem(entity);
  }

  /**
   * Updates an existing client entity in DynamoDB.
   *
   * @param entity the client entity to update.
   * @return the updated client entity.
   */
  public ClientDynamoEntity update(ClientDynamoEntity entity) {
    return clientTable.updateItem(entity);
  }

  /**
   * Finds a client by its unique client ID.
   *
   * @param clientId the client ID.
   * @return the client entity, or {@code null} if not found.
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
   * Finds a client by its ID and visibility status.
   *
   * @param clientId the client ID.
   * @param accessible the visibility status.
   * @return an {@link Optional} containing the client entity, or empty if not found.
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
   * Finds all clients for a specific tenant with pagination support.
   *
   * @param tenantId the tenant ID.
   * @param limit the maximum number of clients to retrieve.
   * @param nextClientId the next client ID for pagination.
   * @param nextCreatedOn the creation timestamp for pagination.
   * @return a list of client entities.
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
   * Finds a client by its client ID and tenant ID.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @return an {@link Optional} containing the client entity, or empty if not found.
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
   * Finds all clients matching the given list of client IDs.
   *
   * @param clientIds the list of client IDs.
   * @return a list of client entities.
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
   * Deletes a client by its client ID and tenant ID.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @return the deleted client entity.
   */
  public ClientDynamoEntity deleteByIdAndTenantId(String clientId, long tenantId) {
    var keyEntity = new ClientDynamoEntity();
    keyEntity.setClientId(clientId);
    keyEntity.setTenantId(tenantId);
    return clientTable.deleteItem(keyEntity);
  }

  /**
   * Updates the client secret for a client.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @param secret the new client secret.
   * @param modifiedOn the modification timestamp.
   * @return the updated client entity.
   */
  public ClientDynamoEntity updateClientSecret(
      String clientId, long tenantId, String secret, ZonedDateTime modifiedOn) {
    var updatedClient = buildUpdatedClient(clientId, tenantId, "clientSecret", secret, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Updates the visibility status for a client.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @param accessible the new visibility status.
   * @param modifiedOn the modification timestamp.
   * @return the updated client entity.
   */
  public ClientDynamoEntity updateVisibility(
      String clientId, long tenantId, boolean accessible, ZonedDateTime modifiedOn) {
    var updatedClient =
        buildUpdatedClient(clientId, tenantId, "accessible", accessible, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Updates the activation status for a client.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @param enabled the new activation status.
   * @param modifiedOn the modification timestamp.
   * @return the updated client entity.
   */
  public ClientDynamoEntity updateActivation(
      String clientId, long tenantId, boolean enabled, ZonedDateTime modifiedOn) {
    var updatedClient = buildUpdatedClient(clientId, tenantId, "enabled", enabled, modifiedOn);
    return clientTable.updateItem(updatedClient);
  }

  /**
   * Builds an updated client entity with modified attributes.
   *
   * @param clientId the client ID.
   * @param tenantId the tenant ID.
   * @param attributeName the name of the attribute to modify.
   * @param value the new value for the attribute.
   * @param modifiedOn the modification timestamp.
   * @return the updated client entity.
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
