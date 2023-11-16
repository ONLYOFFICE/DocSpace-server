/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.request;

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
    @JsonProperty("authentication_method")
    private String authenticationMethod;
    @JsonProperty("website_url")
    private String websiteUrl;
    @JsonProperty("terms_url")
    private String termsUrl;
    @JsonProperty("policy_url")
    private String policyUrl;
    @JsonProperty("redirect_uris")
    private Set<String> redirectUris;
    @JsonProperty("allowed_origins")
    private Set<String> allowedOrigins;
    @JsonProperty("logout_redirect_uri")
    private String logoutRedirectUri;
    private Set<String> scopes;
}
