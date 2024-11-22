// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.common.data.consent.repository;

import com.asc.common.data.consent.entity.ConsentEntity;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * JPA repository interface for performing CRUD operations on {@link ConsentEntity} objects. This
 * interface extends {@link JpaRepository}, providing built-in CRUD functionality and pagination
 * support. Additionally, it defines custom queries for managing and retrieving consent-related
 * data.
 */
public interface JpaConsentRepository
    extends JpaRepository<ConsentEntity, ConsentEntity.ConsentId> {

  /**
   * Retrieves a paginated list of consents associated with a specific principal (user), including
   * the client and scope details, where the consents are not invalidated.
   *
   * @param principalId the unique identifier of the principal (user) whose consents are being
   *     retrieved
   * @param pageable the pagination information
   * @return a page of consents matching the given principal ID
   */
  @EntityGraph(value = "ConsentEntity.withClientAndScopes", type = EntityGraph.EntityGraphType.LOAD)
  @Query(
      "SELECT c FROM ConsentEntity c JOIN c.client cl WHERE c.principalId = :principalId AND c.invalidated = false")
  Page<ConsentEntity> findAllConsentsByPrincipalId(
      @Param("principalId") String principalId, Pageable pageable);

  /**
   * Retrieves a paginated list of consents associated with a specific principal (user) and tenant,
   * including the client and scope details, ensuring the client is not invalidated.
   *
   * @param principalId the unique identifier of the principal (user) whose consents are being
   *     retrieved
   * @param tenant the tenant ID
   * @param pageable the pagination information
   * @return a page of consents matching the given principal ID and tenant ID
   */
  @EntityGraph(value = "ConsentEntity.withClientAndScopes", type = EntityGraph.EntityGraphType.LOAD)
  @Query(
      "SELECT c FROM ConsentEntity c JOIN c.client cl WHERE c.principalId = :principalId AND cl.tenantId = :tenant AND c.invalidated = false")
  Page<ConsentEntity> findAllConsentsByPrincipalIdAndTenant(
      @Param("principalId") String principalId, @Param("tenant") int tenant, Pageable pageable);

  // TODO: Move the logic into interaction with Authorization service
  /**
   * Deletes all consents for a specific principal and client.
   *
   * @param principalId the unique identifier of the principal (user)
   * @param registeredClientId the unique identifier of the client
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
   * Deletes all authorizations for a specific principal and client.
   *
   * @param principalId the unique identifier of the principal (user)
   * @param registeredClientId the unique identifier of the client
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_authorizations WHERE principal_id = :principalId AND registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllAuthorizationsByPrincipalIdAndClientId(
      @Param("principalId") String principalId,
      @Param("registeredClientId") String registeredClientId);

  /**
   * Deletes all consents associated with a specific client.
   *
   * @param registeredClientId the unique identifier of the client
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_consents WHERE registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllConsentsByClientId(@Param("registeredClientId") String registeredClientId);

  /**
   * Deletes all authorizations associated with a specific client.
   *
   * @param registeredClientId the unique identifier of the client
   */
  @Modifying
  @Query(
      value =
          "DELETE FROM identity_authorizations WHERE registered_client_id = :registeredClientId",
      nativeQuery = true)
  void deleteAllAuthorizationsByClientId(@Param("registeredClientId") String registeredClientId);
}
