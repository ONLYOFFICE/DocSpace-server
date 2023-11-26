/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.onlyoffice.authorization.api.external.validation.annotations.EqualsAnySupportedAuthenticationMethod;
import lombok.*;

import java.io.Serializable;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
@ToString
public class UpdateClientDTO implements Serializable {
    private String name;
    private String description;
    @JsonProperty("logo")
    private String logo;
    @JsonProperty("authentication_method")
    @EqualsAnySupportedAuthenticationMethod
    private String authenticationMethod;
}
