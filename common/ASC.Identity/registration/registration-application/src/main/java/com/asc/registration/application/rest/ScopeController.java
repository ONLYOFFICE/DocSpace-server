package com.asc.registration.application.rest;

import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.response.ScopeResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import java.util.LinkedHashSet;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestAttribute;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Controller class for managing scopes. */
@Slf4j
@RestController
@RequestMapping(value = "${web.api}/scopes")
@RequiredArgsConstructor
public class ScopeController {

  /** The service for managing scopes. */
  private final ScopeApplicationService scopeApplicationService;

  /**
   * Retrieves a list of scopes for the specified tenant.
   *
   * @param tenant the tenant information extracted from the request
   * @return a response entity containing an iterable of scope responses
   */
  @GetMapping
  @RateLimiter(name = "globalRateLimiter")
  public ResponseEntity<Iterable<ScopeResponse>> getScopes(
      @RequestAttribute("tenant") AscTenantResponse tenant) {
    MDC.put("tenant_id", String.valueOf(tenant.getTenantId()));
    MDC.put("tenant_alias", tenant.getTenantAlias());
    log.info("Received a request to list scopes");
    MDC.clear();

    return ResponseEntity.ok(
        scopeApplicationService.getScopes().stream()
            .map(
                s ->
                    ScopeResponse.builder()
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
            .collect(Collectors.toCollection(LinkedHashSet::new)));
  }
}
