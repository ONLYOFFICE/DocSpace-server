/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

/**
 *
 */
public interface ClientCleanupUsecases {
    void clientAsyncDeletionTask(String clientId, int tenant);
    boolean deleteClient(String id, int tenantId);
}
