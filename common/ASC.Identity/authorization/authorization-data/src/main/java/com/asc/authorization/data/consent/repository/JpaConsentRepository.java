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
   * Deletes all consents associated with a specific client.
   *
   * @param registeredClientId The unique identifier of the registered client for which the consents
   *     are to be deleted.
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_consents WHERE registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllConsentsByClientId(@Param("registeredClientId") String registeredClientId);

  /**
   * Deletes all consents associated with a specific principal (user).
   *
   * @param principalId The unique identifier of the principal (user) for which the consents are to
   *     be deleted.
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_consents WHERE principal_id = :principalId",
      nativeQuery = true)
  void deleteAllConsentsByPrincipalId(@Param("principalId") String principalId);

  /**
   * Deletes all consents associated with a specific tenant. This method removes consents that are
   * linked to authorizations belonging to the specified tenant.
   *
   * @param tenantId The unique identifier of the tenant for which the consents are to be deleted.
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_consents WHERE (registered_client_id, principal_id) IN ("
              + "SELECT registered_client_id, principal_id FROM identity_authorizations "
              + "WHERE tenant_id = :tenantId)",
      nativeQuery = true)
  void deleteAllConsentsByTenantId(@Param("tenantId") long tenantId);

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
