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
public interface JpaAuthorizationRepository extends JpaRepository<AuthorizationEntity, String> {

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
