package com.onlyoffice.authorization.api.usecases.service.client;

import com.onlyoffice.authorization.api.dto.request.CreateClientDTO;
import com.onlyoffice.authorization.api.dto.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.dto.response.ClientDTO;
import com.onlyoffice.authorization.api.messaging.messages.ClientMessage;

import java.util.List;

public interface ClientCreationUsecases {
    ClientDTO saveClient(ClientMessage message);
    List<String> saveClients(Iterable<ClientMessage> messages);
    ClientDTO clientAsyncCreationTask(CreateClientDTO clientDTO, int tenant);
    ClientDTO updateClient(UpdateClientDTO clientDTO, String clientId, int tenant);
}
