package com.onlyoffice.authorization.api.usecases.service.client;

import com.onlyoffice.authorization.api.dto.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.dto.response.SecretDTO;

public interface ClientMutationUsecases {
    SecretDTO regenerateSecret(String clientId, int tenant);
    boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId);
}
