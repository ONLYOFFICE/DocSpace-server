/**
 *
 */
package com.onlyoffice.authorization.api.core.usecases.service.client;

import org.springframework.cache.annotation.CacheEvict;

/**
 *
 */
public interface ClientCleanupUsecases {
    @CacheEvict(cacheNames = "clients", key = "#clientId")
    void clientAsyncDeletionTask(String clientId, int tenant);
    @CacheEvict(cacheNames = "clients", key = "#id")
    boolean deleteClient(String id, int tenant);
}
