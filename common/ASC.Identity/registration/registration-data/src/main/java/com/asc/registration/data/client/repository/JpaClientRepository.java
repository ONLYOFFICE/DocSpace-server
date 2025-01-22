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

/** JPA repository interface for performing CRUD operations on {@link ClientEntity} objects. */
public interface JpaClientRepository extends JpaRepository<ClientEntity, String> {

  /**
   * Finds a client entity by its ID, provided it is not invalidated.
   *
   * @param id the ID of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  @Nonnull
  @Query("SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false")
  Optional<ClientEntity> findById(@Param("id") @Nonnull String id);

  /**
   * Finds a client entity by its ID and visibility status, provided it is not invalidated.
   *
   * @param id the ID of the client entity
   * @param accessible the visibility status of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  @Nonnull
  @Query(
      "SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false AND c.accessible = :accessible")
  Optional<ClientEntity> findByIdAndVisibility(
      @Param("id") @Nonnull String id, @Param("accessible") boolean accessible);

  /**
   * Finds a client entity by its client_id, provided it is not invalidated.
   *
   * @param clientId the client_id of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  Optional<ClientEntity> findByClientId(String clientId);

  /**
   * Finds a client entity by its client ID and tenant ID.
   *
   * @param clientId the client ID
   * @param tenantId the tenant ID
   * @return an optional containing the found client entity, or empty if not found
   */
  Optional<ClientEntity> findByClientIdAndTenantId(String clientId, long tenantId);

  /** Finds all client entities for a specific tenant using cursor-based pagination. */
  @Query(
      value =
          """
                  SELECT * FROM identity_clients
                  WHERE tenant_id = :tenant
                    AND is_public = true
                    AND is_invalidated = false
                    AND (:lastCreatedOn IS NULL OR created_on < :lastCreatedOn)
                  ORDER BY created_on DESC
                  LIMIT :limit
              """,
      nativeQuery = true)
  List<ClientEntity> findAllByTenantIdWithCursor(
      @Param("tenant") long tenant,
      @Param("lastCreatedOn") ZonedDateTime lastCreatedOn,
      @Param("limit") int limit);

  /**
   * Finds all public clients and private clients belonging to a specific tenant using cursor-based
   * pagination.
   */
  @Query(
      value =
          """
                  SELECT * FROM identity_clients
                  WHERE tenant_id = :tenant
                    AND is_invalidated = false
                    AND (:lastCreatedOn IS NULL OR created_on < :lastCreatedOn)
                  ORDER BY created_on DESC
                  LIMIT :limit
              """,
      nativeQuery = true)
  List<ClientEntity> findAllPublicAndPrivateByTenantWithCursor(
      @Param("tenant") long tenant,
      @Param("lastCreatedOn") ZonedDateTime lastCreatedOn,
      @Param("limit") int limit);

  /**
   * Deletes a client entity by its ID and tenant ID.
   *
   * @param id the ID of the client entity
   * @param tenant the tenant ID
   * @return the number of entities deleted
   */
  int deleteByClientIdAndTenantId(String id, long tenant);

  /**
   * Regenerates the client secret for a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param secret the new client secret
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      """
        UPDATE ClientEntity c
        SET c.clientSecret = :secret, c.modifiedOn = :modifiedOn
        WHERE c.clientId = :clientId AND c.tenantId = :tenant
    """)
  void regenerateClientSecretByClientId(
      @Param("tenant") long tenant,
      @Param("clientId") String clientId,
      @Param("secret") String secret,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Changes the visibility status of a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param accessible the new visibility status
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      """
        UPDATE ClientEntity c
        SET c.accessible = :accessible, c.modifiedOn = :modifiedOn
        WHERE c.clientId = :clientId AND c.tenantId = :tenant
    """)
  void changeVisibility(
      @Param("tenant") long tenant,
      @Param("clientId") String clientId,
      @Param("accessible") boolean accessible,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Changes the activation status of a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param enabled the new activation status
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      """
        UPDATE ClientEntity c
        SET c.enabled = :enabled, c.modifiedOn = :modifiedOn
        WHERE c.clientId = :clientId AND c.tenantId = :tenant
    """)
  void changeActivation(
      @Param("tenant") long tenant,
      @Param("clientId") String clientId,
      @Param("enabled") boolean enabled,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  @Query("SELECT c FROM ClientEntity c WHERE c.clientId IN :clientIds AND c.invalidated = false")
  List<ClientEntity> findAllByClientIds(@Param("clientIds") List<String> clientIds);
}
