package com.onlyoffice.authorization.api.web.server.ports.services.consent;

import com.onlyoffice.authorization.api.core.usecases.repository.consent.ConsentPersistenceRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ConsentDTO;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ConsentMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashSet;
import java.util.Set;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentRetrieveService implements ConsentRetrieveUsecases {
    private final ConsentPersistenceRetrieveUsecases retrieveUsecases;

    /**
     *
     * @param tenant
     * @param principalName
     * @return
     * @throws RuntimeException
     */
    @Transactional(timeout = 2000, readOnly = true)
    public Set<ConsentDTO> getAllByPrincipalName(TenantDTO tenant, String principalName)
            throws RuntimeException {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("principalName", principalName);
        log.info("Trying to get all consents by principal name");
        MDC.clear();

        var response = new HashSet<ConsentDTO>();
        var results = retrieveUsecases.findAllByTenantAndPrincipalName(tenant.getTenantId(),
                principalName);
        results.forEach(r -> response.add(ConsentMapper.INSTANCE.toDTO(r)));
        return response;
    }
}
