package com.onlyoffice.authorization.api.dto.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;

import java.io.Serializable;
import java.util.Set;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
@ToString
public class UpdateClientDTO implements Serializable {
    private String name;
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
}
