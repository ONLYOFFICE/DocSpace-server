/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.authorization;

import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;

/**
 *
 */
public interface AuthorizationRetrieveUsecases {
    /**
     *
     * @param id
     * @return
     */
    OAuth2Authorization findById(String id);

    /**
     *
     * @param token
     * @param tokenType
     * @return
     */
    OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType);
}
