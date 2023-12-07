/**
 *
 */
package com.onlyoffice.authorization.api.web.server.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;
import org.springframework.hateoas.RepresentationModel;

import java.io.Serializable;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class SecretDTO extends RepresentationModel<ClientDTO> implements Serializable {
    @JsonProperty("client_secret")
    private String clientSecret;
}
