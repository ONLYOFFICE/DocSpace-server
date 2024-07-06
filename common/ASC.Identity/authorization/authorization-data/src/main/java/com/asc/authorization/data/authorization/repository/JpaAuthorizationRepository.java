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

package com.asc.authorization.data.authorization.repository;

import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * Repository interface for performing CRUD operations on {@link AuthorizationEntity} objects.
 * Extends {@link JpaRepository}, providing basic CRUD functionality and query methods.
 */
public interface JpaAuthorizationRepository
    extends JpaRepository<AuthorizationEntity, AuthorizationEntity.AuthorizationId> {

  /**
   * Finds an AuthorizationEntity by its authorization ID.
   *
   * @param id The ID of the authorization entity to find.
   * @return An Optional containing the found AuthorizationEntity, or an empty Optional if no entity
   *     with the given ID is found.
   */
  @Query("SELECT a FROM AuthorizationEntity a WHERE a.id = :id")
  Optional<AuthorizationEntity> findByAuthorizationId(String id);

  /**
   * Finds an authorization entity by registered client ID and principal ID.
   *
   * @param registeredClientId the registered client ID
   * @param principalId the principal ID
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
   */
  Optional<AuthorizationEntity> findByRegisteredClientIdAndPrincipalId(
      String registeredClientId, String principalId);

  /**
   * Finds an authorization entity by its state.
   *
   * @param state the state of the authorization entity
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
   */
  Optional<AuthorizationEntity> findByState(String state);

  /**
   * Finds an authorization entity by its authorization code value.
   *
   * @param authorizationCode the authorization code value
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
   */
  Optional<AuthorizationEntity> findByAuthorizationCodeValue(String authorizationCode);

  /**
   * Finds an authorization entity by its access token value.
   *
   * @param accessToken the access token value
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
   */
  Optional<AuthorizationEntity> findByAccessTokenValue(String accessToken);

  /**
   * Finds an authorization entity by its refresh token value.
   *
   * @param refreshToken the refresh token value
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
   */
  Optional<AuthorizationEntity> findByRefreshTokenValue(String refreshToken);

  /**
   * Finds an authorization entity by state, authorization code value, access token value, or
   * refresh token value.
   *
   * @param token the token to search for (can be state, authorization code, access token, or
   *     refresh token)
   * @return an {@link Optional} containing the found authorization entity, or empty if not found
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
}
