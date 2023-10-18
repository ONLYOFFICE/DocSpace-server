/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;
import org.springframework.hateoas.RepresentationModel;

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
public class ClientDTO extends RepresentationModel<ClientDTO> implements Serializable {
    private String name;
    @JsonProperty("client_id")
    private String clientId;
    @JsonProperty("client_secret")
    private String clientSecret;
    private String description;
    @JsonProperty("terms_url")
    private String termsUrl;
    @JsonProperty("policy_url")
    private String policyUrl;
    @JsonProperty("logo_url")
    private String logoUrl;
    @JsonProperty("authentication_method")
    private String authenticationMethod;
    @JsonProperty("redirect_uri")
    private String redirectUri;
    @JsonProperty("logout_redirect_uri")
    private String logoutRedirectUri;
    private Set<String> scopes;
    private int tenant;
    private boolean enabled;
    private boolean invalidated;
}