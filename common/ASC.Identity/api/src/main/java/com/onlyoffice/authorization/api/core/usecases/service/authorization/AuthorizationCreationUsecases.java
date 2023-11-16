/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.authorization;

import com.onlyoffice.authorization.api.core.transfer.messages.AuthorizationMessage;

import java.util.List;

/**
 *
 */
public interface AuthorizationCreationUsecases {
    void saveAuthorization(AuthorizationMessage authorizationMessage);
    List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations);
}
