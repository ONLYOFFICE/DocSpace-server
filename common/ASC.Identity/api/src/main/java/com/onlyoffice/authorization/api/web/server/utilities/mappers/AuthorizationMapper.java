/**
 *
 */
package com.onlyoffice.authorization.api.web.server.utilities.mappers;

import com.onlyoffice.authorization.api.core.entities.Authorization;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import org.mapstruct.Mapper;
import org.mapstruct.MappingTarget;
import org.mapstruct.NullValuePropertyMappingStrategy;
import org.mapstruct.factory.Mappers;

/**
 *
 */
@Mapper(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
public interface AuthorizationMapper {
    AuthorizationMapper INSTANCE = Mappers.getMapper(AuthorizationMapper.class);

    /**
     *
     * @param authorization
     * @return
     */
    AuthorizationMessage toDTO(Authorization authorization);

    /**
     *
     * @param scopeDTO
     * @return
     */
    Authorization toEntity(AuthorizationMessage scopeDTO);

    /**
     *
     * @param entity
     * @param dto
     */
    void update(@MappingTarget Authorization entity, AuthorizationMessage dto);
}
