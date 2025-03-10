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

package com.asc.authorization.data.authorization.repository;

import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * Repository interface for managing {@link AuthorizationEntity} objects in the database.
 *
 * <p>Provides CRUD operations, custom queries, and methods specific to handling authorization data.
 * Extends {@link JpaRepository} to leverage Spring Data JPA functionality.
 */
public interface JpaAuthorizationRepository
    extends JpaRepository<AuthorizationEntity, AuthorizationEntity.AuthorizationId> {

  /**
   * Finds an {@link AuthorizationEntity} by its unique authorization ID.
   *
   * @param id the unique identifier of the authorization entity.
   * @return an {@link Optional} containing the {@link AuthorizationEntity} if found, otherwise
   *     empty.
   */
  @Query("SELECT a FROM AuthorizationEntity a WHERE a.id = :id")
  Optional<AuthorizationEntity> findByAuthorizationId(@Param("id") String id);

  /**
   * Finds an {@link AuthorizationEntity} using a composite key consisting of the registered client
   * ID, principal ID, and authorization grant type.
   *
   * @param registeredClientId the ID of the registered client associated with the authorization.
   * @param principalId the ID of the principal (user or entity) associated with the authorization.
   * @param authorizationGrantType the grant type of the authorization (e.g., "authorization_code").
   * @return an {@link Optional} containing the {@link AuthorizationEntity} if found, otherwise
   *     empty.
   */
  Optional<AuthorizationEntity> findByRegisteredClientIdAndPrincipalIdAndAuthorizationGrantType(
      String registeredClientId, String principalId, String authorizationGrantType);

  /**
   * Finds an {@link AuthorizationEntity} by matching the provided token against various
   * token-related fields.
   *
   * <p>The search is performed against the following fields:
   *
   * <ul>
   *   <li>State
   *   <li>Authorization code value
   *   <li>Access token value
   *   <li>Refresh token value
   *   <li>Access token hash
   *   <li>Refresh token hash
   * </ul>
   *
   * @param token the token value to search for, matching any of the specified fields.
   * @return an {@link Optional} containing the {@link AuthorizationEntity} if a match is found,
   *     otherwise empty.
   */
  @Query(
      "SELECT a FROM AuthorizationEntity a WHERE a.state = :token"
          + " OR a.authorizationCodeValue = :token"
          + " OR a.accessTokenValue = :token"
          + " OR a.refreshTokenValue = :token"
          + " OR a.accessTokenHash = :token"
          + " OR a.refreshTokenHash = :token")
  Optional<AuthorizationEntity>
      findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(
          @Param("token") String token);

  /**
   * Deletes all authorizations for a specific principal and registered client.
   *
   * @param principalId the ID of the principal (user) whose authorizations are to be deleted.
   * @param registeredClientId the ID of the registered client associated with the authorizations.
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
   * Deletes all authorizations associated with a specific registered client.
   *
   * @param clientId the ID of the registered client whose authorizations are to be deleted.
   */
  @Modifying
  @Query(
      value = "DELETE FROM identity_authorizations WHERE registered_client_id = :clientId",
      nativeQuery = true)
  void deleteAllAuthorizationsByClientId(@Param("clientId") String clientId);

  /**
   * Retrieves a list of authorizations for a specific principal, optionally filtered by a last
   * modified date. The query limits results to valid consents (non-empty token fields) and orders
   * them by the most recent modification date.
   *
   * @param principalId the ID of the principal (user) whose authorizations are to be retrieved.
   * @param lastModifiedAt an optional filter to exclude authorizations modified after the specified
   *     date.
   * @param limit the maximum number of authorizations to retrieve.
   * @return a {@link List} of {@link AuthorizationEntity} objects matching the query criteria.
   */
  @Query(
      value =
          """
                SELECT *
                FROM identity_authorizations
                WHERE principal_id = :principalId
                  AND (:lastModifiedAt IS NULL OR modified_at < :lastModifiedAt)
                  AND (
                    (authorization_code_value IS NOT NULL AND authorization_code_value <> '')
                    OR (access_token_value IS NOT NULL AND access_token_value <> '')
                    OR (refresh_token_value IS NOT NULL AND refresh_token_value <> '')
                  )
                  AND authorization_grant_type = 'authorization_code'
                ORDER BY modified_at DESC
                LIMIT :limit
                """,
      nativeQuery = true)
  List<AuthorizationEntity> findConsentedAuthorizationsByPrincipalId(
      @Param("principalId") String principalId,
      @Param("lastModifiedAt") ZonedDateTime lastModifiedAt,
      @Param("limit") int limit);
}
