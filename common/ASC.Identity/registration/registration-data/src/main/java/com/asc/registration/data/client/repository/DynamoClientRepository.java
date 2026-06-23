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
