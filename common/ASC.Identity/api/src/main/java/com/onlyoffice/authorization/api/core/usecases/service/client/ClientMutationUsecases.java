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
    /**
     *
     * @param tenant
     * @param clientId
     * @return
     */
    SecretDTO regenerateSecret(TenantDTO tenant, String clientId);

    /**
     *
     * @param tenant
     * @param activationDTO
     * @param clientId
     * @return
     */
    boolean changeActivation(TenantDTO tenant, ChangeClientActivationDTO activationDTO, String clientId);

    /**
     *
     * @param updateClientPair
     * @return
     */
    Set<String> updateClients(Iterable<Pair<String, ClientMessage>> updateClientPair);

    /**
     *
     * @param tenant
     * @param updateClient
     * @param clientId
     */
    void updateClientAsync(TenantDTO tenant, UpdateClientDTO updateClient, String clientId);
}
