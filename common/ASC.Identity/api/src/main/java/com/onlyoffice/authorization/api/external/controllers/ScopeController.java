/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.core.exceptions.ScopeNotFoundException;
import com.onlyoffice.authorization.api.core.transfer.response.ScopeDTO;
import com.onlyoffice.authorization.api.security.container.TenantContextContainer;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.github.resilience4j.retry.annotation.Retry;
import jakarta.annotation.PostConstruct;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
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

    @PostConstruct
    public void init() {
        this.scopes = configuration.getScopes()
                .stream()
                .map(s -> ScopeDTO
                        .builder()
                        .name(s.getName())
                        .type(s.getType())
                        .group(s.getGroup())
                        .build()
                )
                .sorted((o1, o2)-> {
                    if (o1.getName().equalsIgnoreCase("openid"))
                        return 1;
                    if (o2.getName().equalsIgnoreCase("openid"))
                        return -1;
                    return o1.getName().
                            compareToIgnoreCase(o2.getName());
                })
                .collect(Collectors.toCollection(LinkedHashSet::new));
    }

    @GetMapping
    @Retry(name = "getScopesRetryRateLimiter")
    @RateLimiter(name = "getScopesRateLimiter")
    @SneakyThrows
    public ResponseEntity<Iterable<ScopeDTO>> getScopes() {
        var context = TenantContextContainer.context.get();
        if (context == null)
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        log.info("Received a request to list scopes");
        MDC.clear();
        return ResponseEntity.ok(this.scopes);
    }

    @GetMapping("/{name}")
    @Retry(name = "getScopesRetryRateLimiter")
    @RateLimiter(name = "getScopesRateLimiter")
    @SneakyThrows
    public ResponseEntity<ScopeDTO> getScope(@PathVariable @NotEmpty String name) {
        var context = TenantContextContainer.context.get();
        if (context == null)
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        MDC.put("scope", name);
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        log.info("Received get a specific scope");
        MDC.clear();
        var scope = this.scopes.stream()
                .filter(s -> s.getName().equals(name))
                .findFirst()
                .orElseThrow(() -> new ScopeNotFoundException("could not find scope with this name"));
        return ResponseEntity.ok(scope);
    }
}

