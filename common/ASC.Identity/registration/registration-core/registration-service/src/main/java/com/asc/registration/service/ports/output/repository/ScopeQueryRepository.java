package com.asc.registration.service.ports.output.repository;

import com.asc.registration.core.domain.entity.Scope;
import java.util.Optional;

/**
 * Repository interface for querying scope data from the data store. Provides methods to find a
 * specific scope by name and to find all scopes.
 */
public interface ScopeQueryRepository {

  /**
   * Finds a scope by its name.
   *
   * @param name The name of the scope to find.
   * @return An {@link Optional} containing the found {@link Scope}, or an empty {@link Optional} if
   *     no scope is found.
   */
  Optional<Scope> findByName(String name);

  /**
   * Finds all available scopes.
   *
   * @return An iterable collection of {@link Scope} representing all scopes.
   */
  Iterable<Scope> findAll();
}
