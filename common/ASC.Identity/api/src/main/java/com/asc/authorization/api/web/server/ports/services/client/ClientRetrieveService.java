package com.asc.authorization.api.web.server.ports.services.client;

import com.asc.authorization.api.core.usecases.repository.client.ClientPersistenceRetrievalUsecases;
import com.asc.authorization.api.web.client.transfer.TenantDTO;
import com.asc.authorization.api.web.server.transfer.response.ClientDTO;
import com.asc.authorization.api.web.server.transfer.response.PaginationDTO;
import com.asc.authorization.api.web.server.utilities.mappers.ClientMapper;
import com.asc.authorization.api.core.usecases.service.client.ClientRetrieveUsecases;
import com.asc.authorization.api.web.security.crypto.Cipher;
import jakarta.persistence.EntityNotFoundException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ClientRetrieveService implements ClientRetrieveUsecases {
    private final ClientPersistenceRetrievalUsecases retrievalUsecases;
    private final Cipher cipher;

    /**
     *
     * @param clientId
     * @return
     */
    @Cacheable(cacheNames = {"identityClients"})
    @Transactional(timeout = 1250, readOnly = true)
    public ClientDTO getClient(String clientId) {
        MDC.put("clientId", clientId);
        log.info("Trying to get a valid client by clientId");

        return retrievalUsecases
                .findById(clientId)
                .filter(c -> !c.isInvalidated())
                .map(c -> {
                    try {
                        var query = ClientMapper.INSTANCE.fromEntityToQuery(c);
                        query.setClientSecret(cipher.decrypt(query.getClientSecret()));
                        return query;
                    } catch (Exception e) {
                        throw new EntityNotFoundException(String.
                                format("Could not find and decrypt client secret: %s", e.getMessage()));
                    } finally {
                        MDC.clear();
                    }
                })
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("Could not find client with id %s", clientId)));
    }

    @Cacheable(cacheNames = {"identityClients"}, key = "#clientId")
    @Transactional(timeout = 1250, readOnly = true)
    public ClientDTO getTenantClient(TenantDTO tenant, String clientId) {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Trying to get a tenant client by clientId");
        MDC.clear();

        return retrievalUsecases
                .findClientByClientIdAndTenant(clientId, tenant.getTenantId())
                .filter(c -> !c.isInvalidated())
                .map(c -> {
                    try {
                        var query = ClientMapper.INSTANCE.fromEntityToQuery(c);
                        query.setClientSecret(cipher.decrypt(query.getClientSecret()));
                        return query;
                    } catch (Exception e) {
                        throw new EntityNotFoundException(String.
                                format("Could not find and decrypt client secret: %s", e.getMessage()));
                    } finally {
                        MDC.clear();
                    }
                })
                .orElseThrow(() -> new EntityNotFoundException(String
                        .format("Could not find client with id %s", clientId)));
    }

    /**
     *
     * @param tenant
     * @param page
     * @param limit
     * @return
     */
    @Transactional(timeout = 2250, readOnly = true)
    public PaginationDTO getTenantClients(TenantDTO tenant, int page, int limit) {
        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("page", String.valueOf(page));
        MDC.put("limit", String.valueOf(limit));
        log.info("Trying to get tenant clients", tenant, page, limit);
        MDC.clear();

        var data = retrievalUsecases.findAllByTenant(tenant.getTenantId(), Pageable
                .ofSize(limit).withPage(page));

        var builder = PaginationDTO
                .<ClientDTO>builder()
                .page(page)
                .limit(limit)
                .data(data.stream()
                        .filter(c -> !c.isInvalidated())
                        .map(c -> ClientMapper.INSTANCE.fromEntityToQuery(c))
                        .collect(Collectors.toList()));

        if (data.hasPrevious())
            builder.previous(page - 1);

        if (data.hasNext())
            builder.next(page + 1);

        return builder.build();
    }
}
