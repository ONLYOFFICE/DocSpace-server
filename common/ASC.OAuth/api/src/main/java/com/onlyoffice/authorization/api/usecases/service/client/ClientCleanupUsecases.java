package com.onlyoffice.authorization.api.usecases.service.client;

public interface ClientCleanupUsecases {
    void clientAsyncDeletionTask(String clientId, int tenant);
    boolean deleteClient(String id, int tenantId);
}
