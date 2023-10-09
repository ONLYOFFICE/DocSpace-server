package com.onlyoffice.authorization.api.usecases.service.authorization;

import com.onlyoffice.authorization.api.messaging.messages.AuthorizationMessage;

import java.util.List;

public interface AuthorizationCreationUsecases {
    void saveAuthorization(AuthorizationMessage authorizationMessage);
    List<String> saveAuthorizations(Iterable<AuthorizationMessage> authorizations);
}
