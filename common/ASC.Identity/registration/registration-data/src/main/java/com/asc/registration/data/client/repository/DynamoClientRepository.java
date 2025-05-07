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

import com.asc.registration.data.client.entity.ClientDynamoEntity;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Optional;

/**
 * DynamoDB repository interface for managing {@link ClientDynamoEntity} objects. Provides methods
 * for saving, updating, retrieving, and deleting client entities in DynamoDB.
 */
public interface DynamoClientRepository {

  /**
   * Persists a new client entity to DynamoDB.
   *
   * @param entity the {@link ClientDynamoEntity} to be saved
   */
  void save(ClientDynamoEntity entity);

  /**
   * Updates an existing client entity in DynamoDB.
   *
   * @param entity the {@link ClientDynamoEntity} with updated information
   * @return the updated {@link ClientDynamoEntity}
   */
  ClientDynamoEntity update(ClientDynamoEntity entity);

  /**
   * Retrieves a client entity by its unique identifier.
   *
   * <p>This method returns the client entity if it exists and is valid; otherwise, it may return
   * {@code null}.
   *
   * @param clientId the unique identifier of the client
   * @return the corresponding {@link ClientDynamoEntity} if found, or {@code null} if not found
   */
  ClientDynamoEntity findById(String clientId);

  /**
   * Retrieves a client entity by its unique identifier and accessibility status.
   *
   * @param clientId the unique identifier of the client
   * @param accessible the desired visibility status (true for accessible, false otherwise)
   * @return an {@link Optional} containing the matching {@link ClientDynamoEntity} if found, or an
   *     empty {@link Optional} otherwise
   */
  Optional<ClientDynamoEntity> findByIdAndVisibility(String clientId, boolean accessible);

  /**
   * Retrieves all client entities that match any of the provided client identifiers.
   *
   * @param clientIds a list of client identifiers to search for
   * @return a list of matching {@link ClientDynamoEntity} objects
   */
  List<ClientDynamoEntity> findAllByClientIds(List<String> clientIds);

  /**
   * Retrieves a paginated list of client entities for a specific tenant.
   *
   * <p>Results are limited by the specified {@code limit} and use {@code nextClientId} and {@code
   * nextCreatedOn} as cursor parameters for pagination.
   *
   * @param tenantId the tenant identifier
   * @param limit the maximum number of client entities to return
   * @param nextClientId the client ID to start pagination (cursor), or {@code null} for the first
   *     page
   * @param nextCreatedOn the creation timestamp to start pagination (cursor), or {@code null} for
   *     the first page
   * @return a list of {@link ClientDynamoEntity} objects for the specified tenant
   */
  List<ClientDynamoEntity> findAllByTenantId(
      long tenantId, int limit, String nextClientId, ZonedDateTime nextCreatedOn);

  /**
   * Retrieves a paginated list of client entities created by a specific creator.
   *
   * <p>Results are limited by the specified {@code limit} and use {@code nextClientId} and {@code
   * nextCreatedOn} as cursor parameters for pagination.
   *
   * @param creatorId the identifier of the creator
   * @param limit the maximum number of client entities to return
   * @param nextClientId the client ID to start pagination (cursor), or {@code null} for the first
   *     page
   * @param nextCreatedOn the creation timestamp to start pagination (cursor), or {@code null} for
   *     the first page
   * @return a list of {@link ClientDynamoEntity} objects created by the specified creator
   */
  List<ClientDynamoEntity> findAllByCreatorId(
      String creatorId, int limit, String nextClientId, ZonedDateTime nextCreatedOn);

  /**
   * Retrieves a client entity by its unique identifier and tenant identifier.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @return an {@link Optional} containing the matching {@link ClientDynamoEntity} if found, or an
   *     empty {@link Optional} otherwise
   */
  Optional<ClientDynamoEntity> findByClientIdAndTenantId(String clientId, long tenantId);

  /**
   * Updates the client secret for a specific client entity.
   *
   * <p>The operation updates the client secret and the modification timestamp.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param secret the new client secret
   * @param modifiedOn the timestamp indicating when the update occurred
   * @return the updated {@link ClientDynamoEntity}
   */
  ClientDynamoEntity updateClientSecret(
      String clientId, long tenantId, String secret, ZonedDateTime modifiedOn);

  /**
   * Updates the visibility status of a client entity.
   *
   * <p>The operation updates the accessibility flag and the modification timestamp.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param accessible the new visibility status (true for accessible, false otherwise)
   * @param modifiedOn the timestamp indicating when the update occurred
   * @return the updated {@link ClientDynamoEntity}
   */
  ClientDynamoEntity updateVisibility(
      String clientId, long tenantId, boolean accessible, ZonedDateTime modifiedOn);

  /**
   * Updates the activation status of a client entity.
   *
   * <p>The operation updates the enabled flag and the modification timestamp.
   *
   * @param clientId the unique identifier of the client
   * @param tenantId the tenant identifier
   * @param enabled the new activation status (true if enabled, false otherwise)
   * @param modifiedOn the timestamp indicating when the update occurred
   * @return the updated {@link ClientDynamoEntity}
   */
  ClientDynamoEntity updateActivation(
      String clientId, long tenantId, boolean enabled, ZonedDateTime modifiedOn);

  /**
   * Deletes a client entity by its unique identifier and tenant identifier.
   *
   * @param clientId the unique identifier of the client to be deleted
   * @param tenantId the tenant identifier
   * @return the deleted {@link ClientDynamoEntity} if the deletion was successful, or {@code null}
   *     otherwise
   */
  ClientDynamoEntity deleteByIdAndTenantId(String clientId, long tenantId);

  /**
   * Deletes all client entities created by a specific user within a specific tenant.
   *
   * @param tenantId the tenant identifier
   * @param userId the identifier of the user who created the clients
   */
  void deleteAllByTenantIdAndCreatedBy(long tenantId, String userId);

  /**
   * Deletes all client entities within a specific tenant.
   *
   * @param tenantId the tenant identifier
   */
  void deleteAllByTenantId(long tenantId);
}
