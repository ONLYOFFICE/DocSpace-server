/**
 *
 */
package com.onlyoffice.authorization.api.web.server.utilities.mappers;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.entities.Consent;
import com.onlyoffice.authorization.api.web.server.transfer.messages.ConsentMessage;
import com.onlyoffice.authorization.api.web.server.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ConsentDTO;
import org.mapstruct.Mapper;
import org.mapstruct.Mapping;
import org.mapstruct.MappingTarget;
import org.mapstruct.NullValuePropertyMappingStrategy;
import org.mapstruct.factory.Mappers;

import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import java.util.stream.Collectors;

/**
 *
 */
@Mapper(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
public interface ConsentMapper {
    ConsentMapper INSTANCE = Mappers.getMapper(ConsentMapper.class);

    ConsentMessage toMessage(Consent consent);

    Consent toEntity(ConsentMessage consentMessage);
    @Mapping(source = "invalidated", target = "invalidated")
    ConsentDTO toDTO(Consent consent);
    default Set<ConsentDTO> toDTOs(Set<Consent> consents) {
        var result = new HashSet<ConsentDTO>();
        consents.forEach(c -> result.add(toDTO(c)));
        return result;
    }
    @Mapping(ignore = true, target = "clientSecret")
    ClientDTO clientToDTO(Client client);
    default Set<String> map(String value) {
        return Arrays.stream(value.split(",")).collect(Collectors.toSet());
    }

    void update(@MappingTarget Consent entity, ConsentMessage message);
}
