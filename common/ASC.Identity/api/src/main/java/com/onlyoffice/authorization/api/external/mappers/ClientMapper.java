/**
 *
 */
package com.onlyoffice.authorization.api.external.mappers;

import com.onlyoffice.authorization.api.core.entities.Client;
import com.onlyoffice.authorization.api.core.transfer.messages.ClientMessage;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.ClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.ClientInfoDTO;
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
                    target = "authenticationMethod",
                    defaultValue = "client_secret_post"
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
                    source = "authenticationMethod",
                    target = "authenticationMethod",
                    defaultValue = "client_secret_post"
            )
    })
    ClientMessage fromQueryToMessage(ClientDTO client);
    @Mappings({
            @Mapping(
                    source = "authenticationMethod",
                    target = "authenticationMethod",
                    defaultValue = "client_secret_post"
            )
    })
    ClientDTO fromCommandToQuery(CreateClientDTO client);
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
    })
    ClientDTO fromEntityToQuery(Client client);
    @Mappings({
            @Mapping(source = "enabled", target = "enabled"),
    })
    Client fromQueryToEntity(ClientDTO client);
    @Mappings({
            @Mapping(
                    source = "authenticationMethod",
                    target = "authenticationMethod",
                    defaultValue = "client_secret_post"
            )
    })
    void update(@MappingTarget Client entity, UpdateClientDTO clientDTO);
}