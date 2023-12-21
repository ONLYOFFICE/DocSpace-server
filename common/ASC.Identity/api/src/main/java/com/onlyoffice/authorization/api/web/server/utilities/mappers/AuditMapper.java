package com.onlyoffice.authorization.api.web.server.utilities.mappers;

import com.onlyoffice.authorization.api.core.entities.Audit;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuditMessage;
import org.mapstruct.Mapper;
import org.mapstruct.NullValuePropertyMappingStrategy;
import org.mapstruct.factory.Mappers;

/**
 *
 */
@Mapper(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
public interface AuditMapper {
    AuditMapper INSTANCE = Mappers.getMapper(AuditMapper.class);

    /**
     *
     * @param audit
     * @return
     */
    AuditMessage toDTO(Audit audit);

    /**
     *
     * @param message
     * @return
     */
    Audit toEntity(AuditMessage message);
}
