/**
 *
 */
package com.onlyoffice.authorization.core.usecases.service.authorization;

import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;

/**
 *
 */
public interface AuthorizationCleanupUsecases {
    void remove(OAuth2Authorization authorization);
}