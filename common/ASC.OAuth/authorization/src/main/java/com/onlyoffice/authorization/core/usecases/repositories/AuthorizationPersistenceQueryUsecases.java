/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceQueryUsecases {
    Authorization getById(String id) throws RuntimeException;
    Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId) throws RuntimeException;
    Authorization getByState(String state) throws RuntimeException;
    Authorization getByAuthorizationCodeValue(String authorizationCode) throws RuntimeException;
    Authorization getByAccessTokenValue(String accessToken) throws RuntimeException;
    Authorization getByRefreshTokenValue(String refreshToken) throws RuntimeException;
    Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token);
}
