package com.asc.registration.application.rest;

import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.core.domain.exception.DomainNotFoundException;
import com.asc.registration.application.configuration.ApplicationConfiguration;
import com.asc.registration.application.transfer.ScopeQueryResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.annotation.PostConstruct;
import jakarta.validation.constraints.NotEmpty;
import java.util.LinkedHashSet;
import java.util.Set;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@Slf4j
@RestController
@RequestMapping(value = "${web.api}/scopes")
@RequiredArgsConstructor
public class ScopeController {
  private final ApplicationConfiguration configuration;
  private Set<ScopeQueryResponse> scopes;

  @PostConstruct
  void init() {
    scopes =
        configuration.getScopes().stream()
            .map(
                s ->
                    ScopeQueryResponse.builder()
                        .name(s.getName())
                        .type(s.getType())
                        .group(s.getGroup())
                        .build())
            .sorted(
                (s1, s2) -> {
                  if (s1.getName().equalsIgnoreCase("openid")) return 1;
                  if (s2.getName().equalsIgnoreCase("openid")) return -1;
                  return s1.getName().compareToIgnoreCase(s2.getName());
                })
            .collect(Collectors.toCollection(LinkedHashSet::new));
  }

  @GetMapping
  @RateLimiter(name = "globalRateLimiter")
  public ResponseEntity<Iterable<ScopeQueryResponse>> getScopes(
      @RequestAttribute("tenant") AscTenantResponse tenant) {
    MDC.put("tenant_id", String.valueOf(tenant.getTenantId()));
    MDC.put("tenant_alias", tenant.getTenantAlias());
    log.info("Received a request to list scopes");
    MDC.clear();

    return ResponseEntity.ok(this.scopes);
  }

  @GetMapping("/{name}")
  @RateLimiter(name = "globalRateLimiter")
  public ResponseEntity<ScopeQueryResponse> getScope(
      @RequestAttribute("tenant") AscTenantResponse tenant, @PathVariable @NotEmpty String name) {
    MDC.put("scope", name);
    MDC.put("tenant_id", String.valueOf(tenant.getTenantId()));
    MDC.put("tenant_alias", tenant.getTenantAlias());
    log.info("Received get a specific scope");
    MDC.clear();

    var scope =
        scopes.stream()
            .filter(s -> s.getName().equals(name))
            .findFirst()
            .orElseThrow(() -> new DomainNotFoundException("Could not find scope with this name"));

    return ResponseEntity.ok(scope);
  }
}
