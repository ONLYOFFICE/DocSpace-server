/**
 *
 */
package com.asc.authorization.api.core.usecases.service.authorization;

import com.asc.authorization.api.web.server.messaging.messages.AuthorizationMessage;

import java.util.List;

/**
 *
 */
public interface AuthorizationCreationUsecases {
    /**
     *
     * @param authorizationMessage
     */
    void saveAuthorization(AuthorizationMessage authorizationMessage);

    /**
     *
     * @param authorizations
     * @return
     */
    List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations);
}
