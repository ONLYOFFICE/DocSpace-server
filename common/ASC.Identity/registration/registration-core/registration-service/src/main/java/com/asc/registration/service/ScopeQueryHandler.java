package com.asc.registration.service;

import com.asc.registration.core.domain.exception.ScopeNotFoundException;
import com.asc.registration.service.mapper.ScopeDataMapper;
import com.asc.registration.service.ports.output.repository.ScopeQueryRepository;
import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * ScopeQueryHandler handles query operations related to scopes. It retrieves scope information from
 * the repository and maps it to response objects.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ScopeQueryHandler {
  private final ScopeQueryRepository queryRepository;
  private final ScopeDataMapper dataMapper;

  /**
   * Retrieves a specific scope by its name.
   *
   * @param name the name of the scope to retrieve.
   * @return a {@link ScopeResponse} representing the requested scope.
   * @throws ScopeNotFoundException if the scope with the specified name is not found.
   */
  @Transactional(timeout = 2)
  public ScopeResponse getScope(String name) {
    log.info("Trying to get scope by name: {}", name);

    var scope =
        queryRepository
            .findByName(name)
            .orElseThrow(
                () ->
                    new ScopeNotFoundException(
                        String.format("Scope with name %s was not found", name)));

    return dataMapper.toScopeResponse(scope);
  }

  /**
   * Retrieves all available scopes.
   *
   * @return a set of {@link ScopeResponse} representing all scopes.
   */
  @Transactional(timeout = 2)
  public Set<ScopeResponse> getScopes() {
    log.info("Trying to get scopes");

    return StreamSupport.stream(queryRepository.findAll().spliterator(), false)
        .map(dataMapper::toScopeResponse)
        .collect(Collectors.toSet());
  }
}
