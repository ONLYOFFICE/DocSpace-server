package com.onlyoffice.authorization.api.mappers;

import com.onlyoffice.authorization.api.dto.request.CreateClientDTO;
import com.onlyoffice.authorization.api.dto.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.dto.response.ClientDTO;
import com.onlyoffice.authorization.api.entities.Client;
import com.onlyoffice.authorization.api.messaging.messages.ClientMessage;
import org.bouncycastle.util.Strings;
import org.mapstruct.*;
import org.mapstruct.factory.Mappers;

import java.util.Arrays;
import java.util.Set;
import java.util.stream.Collectors;

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
            )
    })
    Client fromMessageToEntity(ClientMessage message);
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