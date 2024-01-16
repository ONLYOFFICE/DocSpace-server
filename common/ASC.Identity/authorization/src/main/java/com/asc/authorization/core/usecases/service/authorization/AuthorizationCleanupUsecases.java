/**
 *
 */
package com.asc.authorization.core.usecases.service.authorization;

import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;

/**
 *
 */
public interface AuthorizationCleanupUsecases {
    /**
     *
     * @param authorization
     */
    void remove(OAuth2Authorization authorization);
}
