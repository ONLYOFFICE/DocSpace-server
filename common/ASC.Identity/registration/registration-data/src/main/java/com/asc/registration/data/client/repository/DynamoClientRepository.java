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
 * DynamoDB repository interface for performing operations on {@link ClientDynamoEntity} objects.
 */
public interface DynamoClientRepository {

  /**
   * Saves a client entity to DynamoDB.
   *
   * @param entity the client entity to save
   * @return a CompletableFuture indicating the operation result
   */
  void save(ClientDynamoEntity entity);

  /**
   * Updates an existing client entity in DynamoDB.
   *
   * @param entity the client entity to update
   * @return a CompletableFuture indicating the operation result
   */
  ClientDynamoEntity update(ClientDynamoEntity entity);

  /**
   * Finds a client by its ID, provided it is not invalidated.
   *
   * @param clientId the ID of the client entity.
   * @return a CompletableFuture containing the found client entity, or empty if not found.
   */
  ClientDynamoEntity findById(String clientId);

  /**
   * Finds a client entity by its ID and visibility status.
   *
   * @param clientId the ID of the client entity.
   * @param accessible the visibility status of the client entity.
   * @return a CompletableFuture containing the found client entity, or empty if not found.
   */
  Optional<ClientDynamoEntity> findByIdAndVisibility(String clientId, boolean accessible);

  List<ClientDynamoEntity> findAllByClientIds(List<String> clientIds);

  /**
   * Finds all client entities for a specific tenant.
   *
   * @param tenantId the tenant ID.
   * @return a Stream of client entities belonging to the tenant.
   */
  List<ClientDynamoEntity> findAllByTenantId(
      long tenantId, int limit, String nextClientId, ZonedDateTime nextCreatedOn);

  Optional<ClientDynamoEntity> findByClientIdAndTenantId(String clientId, long tenantId);

  /**
   * Updates the client secret for a specific client.
   *
   * @param clientId the ID of the client.
   * @param tenantId the tenant ID.
   * @param secret the new client secret.
   * @param modifiedOn the date and time of modification.
   * @return a CompletableFuture indicating the completion of the update.
   */
  ClientDynamoEntity updateClientSecret(
      String clientId, long tenantId, String secret, ZonedDateTime modifiedOn);

  /**
   * Updates the visibility status of a client entity.
   *
   * @param clientId the ID of the client.
   * @param tenantId the tenant ID.
   * @param accessible the new visibility status.
   * @param modifiedOn the date and time of modification.
   * @return a CompletableFuture indicating the completion of the update.
   */
  ClientDynamoEntity updateVisibility(
      String clientId, long tenantId, boolean accessible, ZonedDateTime modifiedOn);

  /**
   * Updates the activation status of a client entity.
   *
   * @param clientId the ID of the client.
   * @param tenantId the tenant ID.
   * @param enabled the new activation status.
   * @param modifiedOn the date and time of modification.
   * @return a CompletableFuture indicating the completion of the update.
   */
  ClientDynamoEntity updateActivation(
      String clientId, long tenantId, boolean enabled, ZonedDateTime modifiedOn);

  /**
   * Deletes a client entity by its ID and tenant ID.
   *
   * @param clientId the ID of the client entity.
   * @param tenantId the tenant ID.
   * @return a CompletableFuture indicating the completion of the deletion.
   */
  ClientDynamoEntity deleteByIdAndTenantId(String clientId, long tenantId);
}
