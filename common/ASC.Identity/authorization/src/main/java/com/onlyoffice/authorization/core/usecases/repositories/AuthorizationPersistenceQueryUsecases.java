/**
 *
 */
package com.onlyoffice.authorization.core.usecases.repositories;

import com.onlyoffice.authorization.core.entities.Authorization;

/**
 *
 */
public interface AuthorizationPersistenceQueryUsecases {
    Authorization getById(String id);
    Authorization getByPrincipalNameAndRegisteredClientId(String principalName, String registeredClientId);
    Authorization getByState(String state);
    Authorization getByAuthorizationCodeValue(String authorizationCode);
    Authorization getByAccessTokenValue(String accessToken);
    Authorization getByRefreshTokenValue(String refreshToken);
    Authorization getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(String token);
}
