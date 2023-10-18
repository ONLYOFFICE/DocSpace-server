/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.core.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.core.transfer.response.SecretDTO;

/**
 *
 */
public interface ClientMutationUsecases {
    SecretDTO regenerateSecret(String clientId, int tenant);
    boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId);
}
