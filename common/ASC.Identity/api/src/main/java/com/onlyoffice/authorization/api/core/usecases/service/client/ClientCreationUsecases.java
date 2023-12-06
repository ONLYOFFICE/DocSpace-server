/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.core.transfer.messages.ClientMessage;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.ClientDTO;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.Cacheable;

import java.util.List;

/**
 *
 */
public interface ClientCreationUsecases {
    ClientDTO saveClient(ClientMessage message);
    List<String> saveClients(Iterable<ClientMessage> messages);
    ClientDTO createClientAsync(CreateClientDTO clientDTO, int tenant, String tenantUrl);
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    ClientDTO updateClient(UpdateClientDTO clientDTO, String clientId, int tenant);
}
