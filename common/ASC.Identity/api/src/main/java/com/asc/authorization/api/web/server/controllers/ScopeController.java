/**
 *
 */
package com.asc.authorization.api.web.server.controllers;

import com.asc.authorization.api.configuration.ApplicationConfiguration;
import com.asc.authorization.api.extensions.annotations.DistributedRateLimiter;
import com.asc.authorization.api.web.security.context.TenantContextContainer;
import com.asc.authorization.api.web.server.transfer.response.ScopeDTO;
import io.github.resilience4j.retry.annotation.Retry;
import jakarta.annotation.PostConstruct;
import jakarta.persistence.EntityNotFoundException;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashSet;
import java.util.Set;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@RestController
@RequestMapping(value = "/api/2.0/scopes")
@RequiredArgsConstructor
public class ScopeController {
    private final ApplicationConfiguration configuration;
    private Set<ScopeDTO> scopes;

    /**
     *
     */
    @PostConstruct
    public void init() {
        scopes = configuration.getScopes()
                .stream()
                .map(s -> ScopeDTO
                        .builder()
                        .name(s.getName())
                        .type(s.getType())
                        .group(s.getGroup())
                        .build()
                )
                .sorted((s1, s2)-> {
                    if (s1.getName().equalsIgnoreCase("openid"))
                        return 1;
                    if (s2.getName().equalsIgnoreCase("openid"))
                        return -1;
                    return s1.getName().
                            compareToIgnoreCase(s2.getName());
                })
                .collect(Collectors.toCollection(LinkedHashSet::new));
    }

    /**
     *
     * @return
     */
    @GetMapping
    @Retry(name = "getScopesRetryRateLimiter")
    @DistributedRateLimiter(name = "identityFetchScope")
    public ResponseEntity<Iterable<ScopeDTO>> getScopes() {
        var tenant = TenantContextContainer.context.get();
        if (tenant == null)
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();

        MDC.put("tenantId", String.valueOf(tenant.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenant.getResponse().getTenantAlias());
        log.info("Received a request to list scopes");
        MDC.clear();

        return ResponseEntity.ok(this.scopes);
    }

    /**
     *
     * @param name
     * @return
     */
    @GetMapping("/{name}")
    @Retry(name = "getScopesRetryRateLimiter")
    @DistributedRateLimiter(name = "identityFetchScope")
    public ResponseEntity<ScopeDTO> getScope(@PathVariable @NotEmpty String name) {
        var tenant = TenantContextContainer.context.get();
        if (tenant == null)
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();

        MDC.put("scope", name);
        MDC.put("tenantId", String.valueOf(tenant.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenant.getResponse().getTenantAlias());
        log.info("Received get a specific scope");
        MDC.clear();

        var scope = scopes.stream()
                .filter(s -> s.getName().equals(name))
                .findFirst()
                .orElseThrow(() -> new EntityNotFoundException("Could not find scope with this name"));

        return ResponseEntity.ok(scope);
    }
}

