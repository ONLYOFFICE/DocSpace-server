package com.onlyoffice.authorization.api.usecases.service.client;

import com.onlyoffice.authorization.api.dto.response.ClientDTO;
import com.onlyoffice.authorization.api.dto.response.PaginationDTO;

public interface ClientRetrieveUsecases {
    ClientDTO getClient(String clientId, int tenantId);
    PaginationDTO getTenantClients(int tenantId, int page, int limit);
}
