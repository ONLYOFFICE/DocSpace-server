/**
 *
 */
package com.onlyoffice.authorization.api.web.server.utilities.mappers;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.web.server.transfer.messages.ClientMessage;
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

    default Set<String> map(String value) {
        return Arrays.stream(Strings.split(value, ',')).collect(Collectors.toSet());
    }

    default String map(Set<String> value) {
        return String.join(",", value);
    }

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
    ClientInfoDTO fromClientToInfoDTO(ClientDTO client);
    @Mappings({
            @Mapping(
                    source = "authenticationMethods",
                    target = "authenticationMethod"
            )
    })
    ClientMessage fromQueryToMessage(ClientDTO client);
    ClientDTO fromCommandToQuery(CreateClientDTO client);
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
            @Mapping(source = "authenticationMethod", target = "authenticationMethods")
    })
    ClientDTO fromEntityToQuery(Client client);
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
    })
    Client fromQueryToEntity(ClientDTO client);
    void update(@MappingTarget Client entity, UpdateClientDTO clientDTO);
}