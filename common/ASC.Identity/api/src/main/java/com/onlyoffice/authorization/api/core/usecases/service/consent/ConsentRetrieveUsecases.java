package com.onlyoffice.authorization.api.core.usecases.service.consent;

import com.onlyoffice.authorization.api.web.server.transfer.response.ConsentDTO;

import java.util.Set;

public interface ConsentRetrieveUsecases {
    Set<ConsentDTO> getAllByPrincipalName(String principalName) throws RuntimeException;
}
