/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

/**
 *
 */
public interface ClientCleanupUsecases {
    /**
     *
     * @param clientId
     */
    void deleteClientAsync(String clientId);

    /**
     *
     * @param id
     * @param tenant
     * @return
     */
    boolean deleteClient(String id, int tenant);
}
