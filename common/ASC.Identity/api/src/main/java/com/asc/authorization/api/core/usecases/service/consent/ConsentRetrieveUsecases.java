package com.asc.authorization.api.core.usecases.service.consent;

import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.server.transfer.response.ConsentDTO;

import java.util.Set;

/**
 *
 */
public interface ConsentRetrieveUsecases {
    /**
     *
     * @param tenant
     * @param principalName
     * @return
     */
    Set<ConsentDTO> getAllByPrincipalName(TenantDTO tenant, String principalName);
}
