package com.asc.registration.service.ports.input.service;

import com.asc.registration.service.transfer.response.ScopeResponse;
import jakarta.validation.constraints.NotBlank;
import java.util.Set;

/**
 * Service interface for managing and retrieving scopes. Provides methods to get all scopes and to
 * get a specific scope by its name.
 */
public interface ScopeApplicationService {

  /**
   * Retrieves all available scopes.
   *
   * @return an iterable collection of {@link ScopeResponse} representing all scopes.
   */
  Set<ScopeResponse> getScopes();

  /**
   * Retrieves a specific scope by its name.
   *
   * @param name the name of the scope to retrieve.
   * @return a {@link ScopeResponse} representing the requested scope.
   * @throws IllegalArgumentException if the scope name is blank.
   */
  ScopeResponse getScope(@NotBlank String name);
}
