package com.onlyoffice.authorization.api.dto.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;
import org.springframework.hateoas.RepresentationModel;

import java.io.Serializable;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class SecretDTO extends RepresentationModel<ClientDTO> implements Serializable {
    @JsonProperty("client_secret")
    private String clientSecret;
}
