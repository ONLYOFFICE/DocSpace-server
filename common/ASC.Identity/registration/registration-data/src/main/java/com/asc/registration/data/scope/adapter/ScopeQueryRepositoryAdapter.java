package com.asc.registration.data.scope.adapter;

import com.asc.common.data.scope.repository.JpaScopeRepository;
import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.data.scope.mapper.ScopeDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ScopeQueryRepository;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling scope query operations. Implements the {@link ScopeQueryRepository}
 * interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ScopeQueryRepositoryAdapter implements ScopeQueryRepository {
  private final JpaScopeRepository jpaScopeRepository;
  private final ScopeDataAccessMapper scopeDataAccessMapper;

  /**
   * Finds a scope by its name.
   *
   * @param name the name of the scope
   * @return an optional containing the found scope, or empty if not found
   */
  public Optional<Scope> findByName(String name) {
    log.debug("Querying scope by name: {}", name);

    return jpaScopeRepository.findById(name).map(scopeDataAccessMapper::toDomain);
  }

  /**
   * Finds all available scopes.
   *
   * @return a set of all scopes
   */
  public Set<Scope> findAll() {
    log.debug("Querying all scopes");

    return jpaScopeRepository.findAll().stream()
        .map(scopeDataAccessMapper::toDomain)
        .collect(Collectors.toSet());
  }
}
