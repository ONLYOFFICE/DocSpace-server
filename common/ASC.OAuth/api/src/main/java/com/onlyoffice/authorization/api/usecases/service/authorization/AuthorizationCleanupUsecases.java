package com.onlyoffice.authorization.api.usecases.service.authorization;

import com.onlyoffice.authorization.api.messaging.messages.AuthorizationMessage;

public interface AuthorizationCleanupUsecases {
    void deleteAuthorization(AuthorizationMessage a);
    void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations);
}
