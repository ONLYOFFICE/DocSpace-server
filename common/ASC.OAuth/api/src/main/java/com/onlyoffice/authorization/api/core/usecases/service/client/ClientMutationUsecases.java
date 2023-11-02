/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import com.onlyoffice.authorization.api.core.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.core.transfer.response.SecretDTO;
import org.springframework.cache.annotation.CacheEvict;

/**
 *
 */
public interface ClientMutationUsecases {
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    SecretDTO regenerateSecret(String clientId, int tenant);
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    boolean changeActivation(ChangeClientActivationDTO activationDTO, String clientId);
}
