/**
 *
 */
package com.asc.authorization.api.core.usecases.service.client;

import com.asc.authorization.api.web.client.transfer.TenantDTO;

/**
 *
 */
public interface ClientCleanupUsecases {
    /**
     *
     * @param tenant
     * @param clientId
     */
    void deleteClientAsync(TenantDTO tenant, String clientId);

    /**
     *
     * @param tenant
     * @param id
     * @return
     */
    boolean deleteClient(TenantDTO tenant, String id);
}
