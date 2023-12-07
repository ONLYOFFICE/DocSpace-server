/**
 *
 */
package com.onlyoffice.authorization.api.web.server.transfer.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.onlyoffice.authorization.api.extensions.annotations.URLCollection;
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
