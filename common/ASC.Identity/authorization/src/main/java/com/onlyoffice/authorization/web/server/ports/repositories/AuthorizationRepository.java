/**
 *
 */
package com.onlyoffice.authorization.web.server.ports.repositories;

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
    /**
     *
     * @param id
     * @return
     */
    Optional<Authorization> findById(String id);

    /**
     *
     * @param registeredClientId
     * @param principalName
     * @return
     */
    Optional<Authorization> findByRegisteredClientIdAndPrincipalName(String registeredClientId, String principalName);

    /**
     *
     * @param state
     * @return
     */
    Optional<Authorization> findByState(String state);

    /**
     *
     * @param authorizationCode
     * @return
     */
    Optional<Authorization> findByAuthorizationCodeValue(String authorizationCode);

    /**
     *
     * @param accessToken
     * @return
     */
    Optional<Authorization> findByAccessTokenValue(String accessToken);

    /**
     *
     * @param refreshToken
     * @return
     */
    Optional<Authorization> findByRefreshTokenValue(String refreshToken);

    /**
     *
     * @param token
     * @return
     */
    @Query("SELECT a FROM Authorization a WHERE a.state = :token" +
            " OR a.authorizationCodeValue = :token" +
            " OR a.accessTokenValue = :token" +
            " OR a.refreshTokenValue = :token"
    )
    Optional<Authorization> findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(@Param("token") String token);

    /**
     *
     * @param id
     * @return
     * @throws EntityNotFoundException
     */
    default Authorization getById(String id) throws EntityNotFoundException {
        return findById(id)
                .orElse(null);
    }

    /**
     *
     * @param principalName
     * @param registeredClientId
     * @return
     * @throws RuntimeException
     */
    default Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId) throws RuntimeException {
        return findByRegisteredClientIdAndPrincipalName(registeredClientId, principalName)
                .orElse(null);
    }

    /**
     *
     * @param state
     * @return
     * @throws EntityNotFoundException
     */
    default Authorization getByState(String state) throws EntityNotFoundException {
        return findByState(state)
                .orElse(null);
    }

    /**
     *
     * @param authorizationCode
     * @return
     * @throws EntityNotFoundException
     */
    default Authorization getByAuthorizationCodeValue(String authorizationCode) throws EntityNotFoundException {
        return findByAuthorizationCodeValue(authorizationCode)
                .orElse(null);
    }

    /**
     *
     * @param accessToken
     * @return
     * @throws EntityNotFoundException
     */
    default Authorization getByAccessTokenValue(String accessToken) throws EntityNotFoundException {
        return findByAccessTokenValue(accessToken)
                .orElse(null);
    }

    /**
     *
     * @param refreshToken
     * @return
     * @throws EntityNotFoundException
     */
    default Authorization getByRefreshTokenValue(String refreshToken) throws EntityNotFoundException {
        return findByRefreshTokenValue(refreshToken)
                .orElse(null);
    }

    /**
     *
     * @param token
     * @return
     */
    default Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token) {
        return findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token)
                .orElse(null);
    }
}
