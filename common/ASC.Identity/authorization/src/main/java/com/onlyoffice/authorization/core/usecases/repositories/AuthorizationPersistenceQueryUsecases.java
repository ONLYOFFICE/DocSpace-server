/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceQueryUsecases {
    /**
     *
     * @param id
     * @return
     */
    Authorization getById(String id);

    /**
     *
     * @param principalName
     * @param registeredClientId
     * @return
     */
    Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId);

    /**
     *
     * @param state
     * @return
     */
    Authorization getByState(String state);

    /**
     *
     * @param authorizationCode
     * @return
     */
    Authorization getByAuthorizationCodeValue(String authorizationCode);

    /**
     *
     * @param accessToken
     * @return
     */
    Authorization getByAccessTokenValue(String accessToken);

    /**
     *
     * @param refreshToken
     * @return
     */
    Authorization getByRefreshTokenValue(String refreshToken);

    /**
     *
     * @param token
     * @return
     */
    Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token);
}
