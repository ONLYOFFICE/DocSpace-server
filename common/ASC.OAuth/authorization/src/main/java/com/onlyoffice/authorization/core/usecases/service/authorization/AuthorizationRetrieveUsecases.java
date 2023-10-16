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
    OAuth2Authorization findById(String id);
    OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType);
}
