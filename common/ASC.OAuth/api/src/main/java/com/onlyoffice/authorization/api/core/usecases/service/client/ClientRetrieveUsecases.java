/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.core.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.PaginationDTO;

/**
 *
 */
public interface ClientRetrieveUsecases {
    ClientDTO getClient(String clientId, int tenant);
    PaginationDTO getTenantClients(int tenant, int page, int limit);
}
