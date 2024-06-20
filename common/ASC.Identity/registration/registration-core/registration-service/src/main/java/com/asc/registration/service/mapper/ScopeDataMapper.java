package com.asc.registration.service.mapper;

import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.service.transfer.response.ScopeResponse;
import org.springframework.stereotype.Component;

/**
 * Mapper class to convert between {@link Scope} domain entity and {@link ScopeResponse} transfer
 * object.
 */
@Component
public class ScopeDataMapper {

  /**
   * Converts a {@link Scope} domain entity to a {@link ScopeResponse} transfer object.
   *
   * @param scope the scope entity to convert
   * @return the converted scope response
   */
  public ScopeResponse toScopeResponse(Scope scope) {
    if (scope == null) throw new IllegalArgumentException("Scope cannot be null");
    return ScopeResponse.builder()
        .name(scope.getName())
        .group(scope.getGroup())
        .type(scope.getType())
        .build();
  }
}
