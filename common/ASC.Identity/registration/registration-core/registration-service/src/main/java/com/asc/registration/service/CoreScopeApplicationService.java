package com.asc.registration.service;

import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Set;
import lombok.RequiredArgsConstructor;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;
import org.springframework.validation.annotation.Validated;

/**
 * CoreScopeApplicationService implements the {@link ScopeApplicationService} interface, providing
 * core business logic for managing scopes.
 */
@Service
@Validated
@RequiredArgsConstructor
public class CoreScopeApplicationService implements ScopeApplicationService {
  private final ScopeQueryHandler queryHandler;

  /**
   * Retrieves all available scopes.
   *
   * @return a set of {@link ScopeResponse} representing all scopes.
   */
  @Cacheable(value = "scopes", unless = "#result == null")
  public Set<ScopeResponse> getScopes() {
    return queryHandler.getScopes();
  }

  /**
   * Retrieves a specific scope by its name.
   *
   * @param name the name of the scope to retrieve.
   * @return a {@link ScopeResponse} representing the requested scope.
   */
  @Cacheable(value = "scope", key = "#name", unless = "#result == null")
  public ScopeResponse getScope(String name) {
    return queryHandler.getScope(name);
  }
}
