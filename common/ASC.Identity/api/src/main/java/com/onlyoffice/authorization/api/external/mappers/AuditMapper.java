package com.onlyoffice.authorization.api.external.mappers;

import com.onlyoffice.authorization.api.core.entities.Audit;
import com.onlyoffice.authorization.api.core.transfer.messages.AuditMessage;
import org.mapstruct.Mapper;
import org.mapstruct.NullValuePropertyMappingStrategy;
import org.mapstruct.factory.Mappers;

@Mapper(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
public interface AuditMapper {
    AuditMapper INSTANCE = Mappers.getMapper(AuditMapper.class);
    AuditMessage toDTO(Audit audit);
    Audit toEntity(AuditMessage message);
}
