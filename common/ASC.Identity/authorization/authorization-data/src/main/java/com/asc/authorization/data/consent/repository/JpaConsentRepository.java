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

package com.asc.authorization.data.consent.repository;

import com.asc.authorization.data.consent.entity.ConsentEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * JPA repository interface for managing {@link ConsentEntity} objects. This interface extends
 * {@link JpaRepository}, providing built-in CRUD functionality, pagination, and additional custom
 * queries for managing and retrieving consent-related data.
 */
public interface JpaConsentRepository
    extends JpaRepository<ConsentEntity, ConsentEntity.ConsentId> {
  /**
   * Deletes all consents associated with a specific principal (user) and client.
   *
   * @param principalId The unique identifier of the principal (user) for which the consents are to
   *     be deleted.
   * @param registeredClientId The unique identifier of the registered client associated with the
   *     consents.
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_consents WHERE principal_id = :principalId AND registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllConsentsByPrincipalIdAndClientId(
      @Param("principalId") String principalId,
      @Param("registeredClientId") String registeredClientId);

  /**
   * Deletes up to {@code limit} consents associated with a specific client.
   *
   * <p>Bulk removals call this repeatedly until it returns fewer rows than {@code limit}, each call
   * in its own short transaction. Every deleted consent also cascades to its rows in {@code
   * identity_consent_scopes}, so the limit bounds those as well.
   *
   * @param registeredClientId The unique identifier of the registered client for which the consents
   *     are to be deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_consents WHERE registered_client_id = :registeredClientId LIMIT :limit",
      nativeQuery = true)
  int deleteConsentsByClientId(
      @Param("registeredClientId") String registeredClientId, @Param("limit") int limit);

  /**
   * Deletes up to {@code limit} consents associated with a specific principal (user).
   *
   * @param principalId The unique identifier of the principal (user) for which the consents are to
   *     be deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_consents WHERE principal_id = :principalId LIMIT :limit",
      nativeQuery = true)
  int deleteConsentsByPrincipalId(
      @Param("principalId") String principalId, @Param("limit") int limit);

  /**
   * Deletes up to {@code limit} consents given by the users of a specific tenant. Consents do not
   * record the tenant of the consenting user, so they are found through the tenant's
   * authorizations; consents given to the tenant's own clients are removed by owner instead.
   *
   * <p>The inner query is wrapped in a derived table so that MySQL materializes it, which both
   * allows reading {@code identity_consents} while deleting from it and allows the {@code LIMIT}.
   *
   * @param tenantId The unique identifier of the tenant whose users' consents are to be deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  @Modifying
  @Query(
      value =
          """
          DELETE FROM identity_consents
          WHERE (registered_client_id, principal_id) IN (
            SELECT registered_client_id, principal_id FROM (
              SELECT c.registered_client_id, c.principal_id
              FROM identity_authorizations a
              JOIN identity_consents c
                ON c.registered_client_id = a.registered_client_id
               AND c.principal_id = a.principal_id
              WHERE a.tenant_id = :tenantId
              LIMIT :limit
            ) legacy)
          """,
      nativeQuery = true)
  int deleteConsentsByTenantId(@Param("tenantId") long tenantId, @Param("limit") int limit);

  /**
   * Deletes up to {@code limit} consents given to a client owned by a specific tenant, whichever
   * tenant the consenting user belonged to.
   *
   * @param ownerTenantId The ID of the tenant owning the clients.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_consents WHERE owner_tenant_id = :ownerTenantId LIMIT :limit",
      nativeQuery = true)
  int deleteConsentsByOwnerTenantId(
      @Param("ownerTenantId") long ownerTenantId, @Param("limit") int limit);

  /**
   * Deletes up to {@code limit} consents given to a client created by a specific user, whichever
   * tenant the consenting user belonged to.
   *
   * @param ownerTenantId The ID of the tenant owning the clients.
   * @param ownerUserId The ID of the user who created the clients.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_consents WHERE owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId LIMIT :limit",
      nativeQuery = true)
  int deleteConsentsByOwner(
      @Param("ownerTenantId") long ownerTenantId,
      @Param("ownerUserId") String ownerUserId,
      @Param("limit") int limit);

  /**
   * Deletes all authorizations associated with a specific client.
   *
   * @param registeredClientId The unique identifier of the registered client for which the
   *     authorizations are to be deleted.
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_authorizations WHERE registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllAuthorizationsByClientId(@Param("registeredClientId") String registeredClientId);
}
