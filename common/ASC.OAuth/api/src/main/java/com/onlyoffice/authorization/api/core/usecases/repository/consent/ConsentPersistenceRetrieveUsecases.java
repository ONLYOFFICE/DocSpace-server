package com.onlyoffice.authorization.api.core.usecases.repository.consent;

import com.onlyoffice.authorization.api.core.entities.Consent;

import java.util.Set;

public interface ConsentPersistenceRetrieveUsecases {
    Set<Consent> findAllByPrincipalName(String principalName);
}
