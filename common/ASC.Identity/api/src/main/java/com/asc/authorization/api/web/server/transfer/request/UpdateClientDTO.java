/**
 *
 */
package com.asc.authorization.api.web.server.transfer.request;

import com.asc.authorization.api.extensions.annotations.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
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
