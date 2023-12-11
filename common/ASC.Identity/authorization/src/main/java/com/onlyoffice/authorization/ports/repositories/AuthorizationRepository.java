/**
 *
 */
package com.onlyoffice.authorization.ports.repositories;

import com.onlyoffice.authorization.core.entities.Authorization;
import com.onlyoffice.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.exceptions.EntityNotFoundException;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.Repository;
import org.springframework.data.repository.query.Param;

import java.util.Optional;

/**
 *
 */
public interface AuthorizationRepository extends Repository<Authorization, String>,
        AuthorizationPersistenceQueryUsecases {
    Optional<Authorization> findById(String id);
    Optional<Authorization> findByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName);
    Optional<Authorization> findByState(String state);
    Optional<Authorization> findByAuthorizationCodeValue(String authorizationCode);
    Optional<Authorization> findByAccessTokenValue(String accessToken);
    Optional<Authorization> findByRefreshTokenValue(String refreshToken);
    @Query("SELECT a FROM Authorization a WHERE a.state = :token" +
            " OR a.authorizationCodeValue = :token" +
            " OR a.accessTokenValue = :token" +
            " OR a.refreshTokenValue = :token"
    )
    Optional<Authorization> findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(@Param("token") String token);

    default Authorization getById(String id) throws EntityNotFoundException {
        return this.findById(id)
                .orElse(null);
    }

    default Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId) throws RuntimeException {
        return this.findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElse(null);
    }

    default Authorization getByState(String state) throws EntityNotFoundException {
        return this.findByState(state)
                .orElse(null);
    }

    default Authorization getByAuthorizationCodeValue(String authorizationCode) throws EntityNotFoundException {
        return this.findByAuthorizationCodeValue(authorizationCode)
                .orElse(null);
    }

    default Authorization getByAccessTokenValue(String accessToken) throws EntityNotFoundException {
        return this.findByAccessTokenValue(accessToken)
                .orElse(null);
    }

    default Authorization getByRefreshTokenValue(String refreshToken) throws EntityNotFoundException {
        return this.findByRefreshTokenValue(refreshToken)
                .orElse(null);
    }

    default Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token) {
        return this.findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token)
                .orElse(null);
    }
}
