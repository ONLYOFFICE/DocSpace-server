/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.web.client.transfer.TenantDTO;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.SecretDTO;
import org.springframework.data.util.Pair;

import java.util.Set;

/**
 *
 */
public interface ClientMutationUsecases {
    SecretDTO regenerateSecret(String clientId, TenantDTO tenant);
    boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId);
    void updateClient(UpdateClientDTO clientDTO, String clientId, int tenant);
    Set<String> updateClients(Iterable<Pair<String, ClientMessage>> updateClientPair);
    void updateClientAsync(UpdateClientDTO updateClient, String clientId);
}
