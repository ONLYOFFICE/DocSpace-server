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
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with id %s", id)));
    }

    default Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId) throws RuntimeException {
        return this.findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with principal %s and client id %s", principalName, registeredClientId)));
    }

    default Authorization getByState(String state) throws EntityNotFoundException {
        return this.findByState(state)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with state %s", state)));
    }

    default Authorization getByAuthorizationCodeValue(String authorizationCode) throws EntityNotFoundException {
        return this.findByAuthorizationCodeValue(authorizationCode)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with auth code %s", authorizationCode)));
    }

    default Authorization getByAccessTokenValue(String accessToken) throws EntityNotFoundException {
        return this.findByAccessTokenValue(accessToken)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with access token %s", accessToken)));
    }

    default Authorization getByRefreshTokenValue(String refreshToken) throws EntityNotFoundException {
        return this.findByRefreshTokenValue(refreshToken)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with refresh token %s", refreshToken)));
    }

    default Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token) {
        return this.findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token)
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("could not find authorization with parameter %s", token)));
    }
}
