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

import com.asc.registration.data.client.entity.ClientEntity;
import jakarta.annotation.Nonnull;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * JPA repository interface for managing {@link ClientEntity} objects. Provides CRUD operations as
 * well as custom queries to retrieve, update, and delete clients based on tenant, visibility, and
 * pagination criteria.
 */
public interface JpaClientRepository extends JpaRepository<ClientEntity, String> {

  /**
   * Retrieves a client entity by its unique identifier, provided that the entity has not been
   * invalidated.
   *
   * @param id the unique identifier of the client entity
   * @return an {@link Optional} containing the found {@link ClientEntity} if it exists and is
   *     valid, otherwise an empty {@link Optional}
   */
  @Nonnull
  @Query("SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false")
  Optional<ClientEntity> findById(@Param("id") @Nonnull String id);

  /**
   * Retrieves a client entity by its unique identifier and accessibility status, ensuring that the
   * entity has not been invalidated.
   *
   * @param id the unique identifier of the client entity
   * @param accessible the desired accessibility status (true for accessible, false otherwise)
   * @return an {@link Optional} containing the matching {@link ClientEntity} if found, otherwise an
   *     empty {@link Optional}
   */
  @Nonnull
  @Query(
      "SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false AND c.accessible = :accessible")
  Optional<ClientEntity> findByIdAndVisibility(
      @Param("id") @Nonnull String id, @Param("accessible") boolean accessible);

  /**
   * Retrieves a client entity by its client identifier and tenant identifier.
   *
   * @param clientId the client identifier
   * @param tenantId the tenant identifier
   * @return an {@link Optional} containing the matching {@link ClientEntity} if present, otherwise
   *     an empty {@link Optional}
   */
  Optional<ClientEntity> findByClientIdAndTenantId(String clientId, long tenantId);

  /**
   * Retrieves a client entity by its client identifier, tenant identifier, and creator identifier.
   *
   * @param clientId the client identifier
   * @param tenantId the tenant identifier
   * @param createdBy the identifier of the creator of the client entity
   * @return an {@link Optional} containing the matching {@link ClientEntity} if found, otherwise an
   *     empty {@link Optional}
   */
  Optional<ClientEntity> findByClientIdAndTenantIdAndCreatedBy(
      String clientId, long tenantId, String createdBy);

  /**
   * Retrieves a paginated list of both public and private client entities for a specified tenant.
   *
   * <p>This query employs cursor-based pagination using the creation timestamp. When {@code
   * lastCreatedOn} is provided, only client entities created before this timestamp are returned.
   *
   * @param tenantId the tenant identifier
   * @param lastCreatedOn the cursor timestamp for pagination (may be {@code null} to fetch the most
   *     recent records)
   * @param limit the maximum number of client entities to return
   * @return a list of matching {@link ClientEntity} objects
   */
  @Query(
      value =
          """
                          SELECT * FROM identity_clients
                          WHERE tenant_id = :tenantId
                            AND is_invalidated = false
                            AND (:lastCreatedOn IS NULL OR created_on < :lastCreatedOn)
                          ORDER BY created_on DESC
                          LIMIT :limit
                      """,
      nativeQuery = true)
  List<ClientEntity> findAllByTenantIdWithCursor(
      @Param("tenantId") long tenantId,
      @Param("lastCreatedOn") ZonedDateTime lastCreatedOn,
      @Param("limit") int limit);

  /**
   * Retrieves a paginated list of both public and private client entities for a specified tenant,
   * filtered by the creator's identifier.
   *
   * <p>This query employs cursor-based pagination using the creation timestamp. When {@code
   * lastCreatedOn} is provided, only client entities created before this timestamp are returned.
   *
   * @param tenantId the tenant identifier
   * @param createdBy the identifier of the creator of the client entities
   * @param lastCreatedOn the cursor timestamp for pagination (may be {@code null} to fetch the most
   *     recent records)
   * @param limit the maximum number of client entities to return
   * @return a list of matching {@link ClientEntity} objects
   */
  @Query(
      value =
          """
                                  SELECT * FROM identity_clients
                                  WHERE tenant_id = :tenantId
                                    AND is_invalidated = false
                                    AND (:lastCreatedOn IS NULL OR created_on < :lastCreatedOn)
                                    AND created_by = :createdBy
                                  ORDER BY created_on DESC
                                  LIMIT :limit
                              """,
      nativeQuery = true)
  List<ClientEntity> findAllByTenantIdAndCreatedByWithCursor(
      @Param("tenantId") long tenantId,
      @Param("createdBy") String createdBy,
      @Param("lastCreatedOn") ZonedDateTime lastCreatedOn,
      @Param("limit") int limit);

  /**
   * Deletes a client entity matching the specified client identifier and tenant identifier.
   *
   * @param id the client identifier
   * @param tenantId the tenant identifier
   * @return the number of client entities deleted (typically 0 or 1)
   */
  int deleteByClientIdAndTenantId(String id, long tenantId);

  /**
   * Updates the client secret for a specified client entity.
   *
   * <p>This operation sets a new client secret and updates the modification timestamp.
   *
   * @param tenantId the tenant identifier associated with the client entity
   * @param clientId the client identifier
   * @param secret the new client secret to set
   * @param modifiedOn the timestamp indicating when the update occurred
   */
  @Modifying
  @Query(
      """
            UPDATE ClientEntity c
            SET c.clientSecret = :secret, c.modifiedOn = :modifiedOn
            WHERE c.clientId = :clientId AND c.tenantId = :tenantId
        """)
  void regenerateClientSecretByClientId(
      @Param("tenantId") long tenantId,
      @Param("clientId") String clientId,
      @Param("secret") String secret,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Updates the accessibility status of a specified client entity.
   *
   * <p>This method changes the visibility of the client entity and updates the modification
   * timestamp.
   *
   * @param tenantId the tenant identifier associated with the client entity
   * @param clientId the client identifier
   * @param accessible the new accessibility status (true for accessible, false otherwise)
   * @param modifiedOn the timestamp indicating when the update occurred
   */
  @Modifying
  @Query(
      """
            UPDATE ClientEntity c
            SET c.accessible = :accessible, c.modifiedOn = :modifiedOn
            WHERE c.clientId = :clientId AND c.tenantId = :tenantId
        """)
  void changeVisibility(
      @Param("tenantId") long tenantId,
      @Param("clientId") String clientId,
      @Param("accessible") boolean accessible,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Updates the activation status of a specified client entity.
   *
   * <p>This method toggles the enabled state of the client entity and updates the modification
   * timestamp.
   *
   * @param tenantId the tenant identifier associated with the client entity
   * @param clientId the client identifier
   * @param enabled the new activation status (true if enabled, false otherwise)
   * @param modifiedOn the timestamp indicating when the update occurred
   */
  @Modifying
  @Query(
      """
            UPDATE ClientEntity c
            SET c.enabled = :enabled, c.modifiedOn = :modifiedOn
            WHERE c.clientId = :clientId AND c.tenantId = :tenantId
        """)
  void changeActivation(
      @Param("tenantId") long tenantId,
      @Param("clientId") String clientId,
      @Param("enabled") boolean enabled,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Retrieves a list of client entities whose identifiers are included in the provided list,
   * filtering out any entities that have been invalidated.
   *
   * @param clientIds a list of client identifiers to search for
   * @return a list of matching {@link ClientEntity} objects
   */
  @Query("SELECT c FROM ClientEntity c WHERE c.clientId IN :clientIds AND c.invalidated = false")
  List<ClientEntity> findAllByClientIds(@Param("clientIds") List<String> clientIds);
}
