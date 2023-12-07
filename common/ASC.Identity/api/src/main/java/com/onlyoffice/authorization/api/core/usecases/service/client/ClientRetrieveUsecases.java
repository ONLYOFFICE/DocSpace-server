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
    ClientDTO getClient(String clientId);
    PaginationDTO getTenantClients(int tenant, int page, int limit);
}
