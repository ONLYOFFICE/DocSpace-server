package com.asc.registration.data.scope.mapper;

import com.asc.common.data.scope.entity.ScopeEntity;
import com.asc.registration.core.domain.entity.Scope;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

/**
 * Mapper class for converting between {@link ScopeEntity} data access objects and {@link Scope}
 * domain objects.
 */
@Component
@RequiredArgsConstructor
public class ScopeDataAccessMapper {

  /**
   * Converts a {@link ScopeEntity} data access object to a {@link Scope} domain object.
   *
   * @param entity the data access object to convert
   * @return the converted domain object
   */
  public Scope toDomain(ScopeEntity entity) {
    return Scope.Builder.builder()
        .name(entity.getName())
        .group(entity.getGroup())
        .type(entity.getType())
        .build();
  }
}
