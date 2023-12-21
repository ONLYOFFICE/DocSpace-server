/**
 *
 */
package com.onlyoffice.authorization.api.web.server.utilities.mappers;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ClientMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.ClientInfoDTO;
import org.bouncycastle.util.Strings;
import org.mapstruct.*;
import org.mapstruct.factory.Mappers;

import java.util.Arrays;
import java.util.Set;
import java.util.stream.Collectors;

/**
 *
 */
@Mapper(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
public interface ClientMapper {
    ClientMapper INSTANCE = Mappers.getMapper(ClientMapper.class);

    /**
     *
     * @param value
     * @return
     */
    default Set<String> map(String value) {
        return Arrays.stream(Strings.split(value, ',')).collect(Collectors.toSet());
    }

    /**
     *
     * @param value
     * @return
     */
    default String map(Set<String> value) {
        if (value == null) return null;
        return String.join(",", value);
    }

    /**
     *
     * @param message
     * @return
     */
    @Mappings({
            @Mapping(
                    source = "authenticationMethod",
                    target = "authenticationMethod"
            ),
            @Mapping(
                    source = "createdOn",
                    target = "createdOn"
            ),
            @Mapping(
                    source = "modifiedOn",
                    target = "modifiedOn"
            ),
            @Mapping(
                    source = "websiteUrl",
                    target = "websiteUrl"
            ),
            @Mapping(
                    source = "allowedOrigins",
                    target = "allowedOrigins"
            )
    })
    Client fromMessageToEntity(ClientMessage message);

    /**
     *
     * @param client
     * @return
     */
    ClientInfoDTO fromClientToInfoDTO(ClientDTO client);

    /**
     *
     * @param client
     * @return
     */
    @Mappings({
            @Mapping(
                    source = "authenticationMethods",
                    target = "authenticationMethod"
            )
    })
    ClientMessage fromQueryToMessage(ClientDTO client);

    /**
     *
     * @param client
     * @return
     */
    ClientDTO fromCommandToQuery(CreateClientDTO client);

    /**
     *
     * @param client
     * @return
     */
    @Mappings({
            @Mapping(
                    source = "allowPkce",
                    target = "allowPkce"
            )
    })
    ClientMessage fromCommandToMessage(UpdateClientDTO client);

    /**
     *
     * @param client
     * @return
     */
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
            @Mapping(source = "authenticationMethod", target = "authenticationMethods")
    })
    ClientDTO fromEntityToQuery(Client client);

    /**
     *
     * @param client
     * @return
     */
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
    })
    Client fromQueryToEntity(ClientDTO client);

    /**
     *
     * @param entity
     * @param clientDTO
     */
    @Mappings({
            @Mapping(target = "enabled", ignore = true),
            @Mapping(target = "invalidated", ignore = true),
            @Mapping(target = "tenant", ignore = true)
    })
    void update(@MappingTarget Client entity, UpdateClientDTO clientDTO);

    /**
     *
     * @param entity
     * @param secondEntity
     */
    @Mappings({
            @Mapping(target = "enabled", ignore = true),
            @Mapping(target = "invalidated", ignore = true),
            @Mapping(target = "tenant", ignore = true)
    })
    void update(@MappingTarget Client entity, Client secondEntity);
}