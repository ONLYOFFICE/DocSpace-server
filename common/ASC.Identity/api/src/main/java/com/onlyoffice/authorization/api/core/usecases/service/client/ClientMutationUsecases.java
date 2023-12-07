/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.SecretDTO;

/**
 *
 */
public interface ClientMutationUsecases {
    SecretDTO regenerateSecret(String clientId, int tenant);
    boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId);
}
