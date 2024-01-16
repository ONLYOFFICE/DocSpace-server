/**
 *
 */
package com.asc.authorization.api.core.usecases.service.client;

import com.asc.authorization.api.web.client.transfer.PersonDTO;
import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.server.messaging.messages.ClientMessage;
import com.asc.authorization.api.web.server.transfer.response.ClientDTO;
import com.asc.authorization.api.web.server.transfer.request.CreateClientDTO;

import java.util.List;

/**
 *
 */
public interface ClientCreationUsecases {
    /**
     *
     * @param message
     * @return
     */
    ClientDTO saveClient(ClientMessage message);

    /**
     *
     * @param messages
     * @return
     */
    List<String> saveClients(Iterable<ClientMessage> messages);

    /**
     *
     * @param tenant
     * @param clientDTO
     * @param person
     * @param tenantUrl
     * @return
     */
    ClientDTO createClientAsync(TenantDTO tenant, CreateClientDTO clientDTO,
                                PersonDTO person, String tenantUrl);
}
