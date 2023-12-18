package com.onlyoffice.authorization.api.web.server.ports.services.consent;

import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.web.server.transfer.response.ConsentDTO;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashSet;
import java.util.Set;

@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentRetrieveService implements ConsentRetrieveUsecases {
    private final ConsentPersistenceRetrieveUsecases retrieveUsecases;

    @Transactional(readOnly = true, rollbackFor = Exception.class, timeout = 2000)
    public Set<ConsentDTO> getAllByPrincipalName(String principalName)
            throws RuntimeException {
        MDC.put("principalName", principalName);
        log.info("Trying to get all consents by principal name");
        MDC.clear();

        var response = new HashSet<ConsentDTO>();
        var results = retrieveUsecases.findAllByPrincipalName(principalName);
        results.forEach(r -> response.add(ConsentMapper.INSTANCE.toDTO(r)));
        return response;
    }
}
