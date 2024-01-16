/**
 *
 */
package com.asc.authorization.api.core.usecases.service.authorization;

import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.server.messaging.messages.AuthorizationMessage;

/**
 *
 */
public interface AuthorizationCleanupUsecases {
    /**
     *
     * @param a
     */
    void deleteAuthorization(AuthorizationMessage a);

    /**
     *
     * @param authorizations
     */
    void deleteAuthorizations(Iterable<AuthorizationMessage> authorizations);

    /**
     *
     * @param registeredClientId
     * @return
     */
    int deleteAuthorizationsByClientId(String registeredClientId);

    /**
     *
     * @param tenant
     * @param registeredClientId
     * @return
     */
    int deleteTenantAuthorizationsByClientId(TenantDTO tenant, String registeredClientId);
}
