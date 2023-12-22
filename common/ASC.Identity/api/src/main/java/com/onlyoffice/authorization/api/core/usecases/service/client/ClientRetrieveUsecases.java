/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.web.server.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.PaginationDTO;

/**
 *
 */
public interface ClientRetrieveUsecases {
    /**
     *
     * @param clientId
     * @return
     */
    ClientDTO getClient(String clientId);

    /**
     *
     * @param tenant
     * @param page
     * @param limit
     * @return
     */
    PaginationDTO getTenantClients(int tenant, int page, int limit);
}