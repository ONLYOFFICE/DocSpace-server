/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.core.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.PaginationDTO;
import org.springframework.cache.annotation.Cacheable;

/**
 *
 */
public interface ClientRetrieveUsecases {
    @Cacheable("clients")
    ClientDTO getClient(String clientId);
    PaginationDTO getTenantClients(int tenant, int page, int limit);
}
