/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.authorization;

import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;

/**
 *
 */
public interface AuthorizationCleanupUsecases {
    void deleteAuthorization(AuthorizationMessage a);
    void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations);
    int deleteAuthorizationsByClientId(String registeredClientId);
}
