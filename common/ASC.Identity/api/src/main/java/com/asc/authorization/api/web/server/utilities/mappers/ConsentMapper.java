/**
 *
 */
package com.asc.authorization.api.web.server.utilities.mappers;

import com.asc.authorization.api.web.server.messaging.messages.ConsentMessage;
import com.asc.authorization.api.web.server.transfer.response.ClientDTO;
import com.asc.authorization.api.web.server.transfer.response.ConsentDTO;
import com.asc.authorization.api.core.entities.Client;
import com.asc.authorization.api.core.entities.Consent;
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

    /**
     *
     * @param consent
     * @return
     */
    ConsentMessage toMessage(Consent consent);

    /**
     *
     * @param consentMessage
     * @return
     */
    Consent toEntity(ConsentMessage consentMessage);

    /**
     *
     * @param consent
     * @return
     */
    @Mapping(source = "invalidated", target = "invalidated")
    ConsentDTO toDTO(Consent consent);

    /**
     *
     * @param consents
     * @return
     */
    default Set<ConsentDTO> toDTOs(Set<Consent> consents) {
        var result = new HashSet<ConsentDTO>();
        consents.forEach(c -> result.add(toDTO(c)));
        return result;
    }

    /**
     *
     * @param client
     * @return
     */
    @Mapping(ignore = true, target = "clientSecret")
    ClientDTO clientToDTO(Client client);

    /**
     *
     * @param value
     * @return
     */
    default Set<String> map(String value) {
        return Arrays.stream(value.split(",")).collect(Collectors.toSet());
    }

    /**
     *
     * @param entity
     * @param message
     */
    void update(@MappingTarget Consent entity, ConsentMessage message);
}
