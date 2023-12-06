/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.onlyoffice.authorization.api.external.validation.annotations.EqualsAnySupportedAuthenticationMethod;
import com.onlyoffice.authorization.api.external.validation.annotations.URLCollection;
import lombok.*;

import java.io.Serializable;
import java.util.Set;

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
    @JsonProperty("allow_pkce")
    private boolean allowPkce;
    @JsonProperty("allowed_origins")
    @URLCollection
    private Set<String> allowedOrigins;
}
