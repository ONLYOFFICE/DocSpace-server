package com.asc.authorization.data.authorization.repository;

import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface JpaAuthorizationRepository extends JpaRepository<AuthorizationEntity, String> {

  /**
   * Finds an authorization entity by its ID.
   *
   * @param id the ID of the authorization entity.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findById(String id);

  /**
   * Finds an authorization entity by registered client ID and principal name.
   *
   * @param registeredClientId the registered client ID.
   * @param principalName the principal name.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findByRegisteredClientIdAndPrincipalName(
      String registeredClientId, String principalName);

  /**
   * Finds an authorization entity by its state.
   *
   * @param state the state of the authorization entity.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findByState(String state);

  /**
   * Finds an authorization entity by its authorization code value.
   *
   * @param authorizationCode the authorization code value.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findByAuthorizationCodeValue(String authorizationCode);

  /**
   * Finds an authorization entity by its access token value.
   *
   * @param accessToken the access token value.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findByAccessTokenValue(String accessToken);

  /**
   * Finds an authorization entity by its refresh token value.
   *
   * @param refreshToken the refresh token value.
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  Optional<AuthorizationEntity> findByRefreshTokenValue(String refreshToken);

  /**
   * Finds an authorization entity by state, authorization code value, access token value, or
   * refresh token value.
   *
   * @param token the token to search for (can be state, authorization code, access token, or
   *     refresh token).
   * @return an Optional containing the found authorization entity, or empty if not found.
   */
  @Query(
      "SELECT a FROM AuthorizationEntity a WHERE a.state = :token"
          + " OR a.authorizationCodeValue = :token"
          + " OR a.accessTokenValue = :token"
          + " OR a.refreshTokenValue = :token")
  Optional<AuthorizationEntity>
      findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(
          @Param("token") String token);
}
