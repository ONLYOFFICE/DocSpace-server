/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.*;
import org.springframework.hateoas.RepresentationModel;

import java.io.Serializable;
import java.sql.Timestamp;
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
    @JsonProperty("website_url")
    private String websiteUrl;
    @JsonProperty("terms_url")
    private String termsUrl;
    @JsonProperty("policy_url")
    private String policyUrl;
    @JsonProperty("logo")
    private String logo;
    @JsonProperty("authentication_method")
    private String authenticationMethod;
    private int tenant;
    @JsonProperty("redirect_uris")
    private Set<String> redirectUris;
    @JsonProperty("allowed_origins")
    private Set<String> allowedOrigins;
    @JsonProperty("logout_redirect_uris")
    private Set<String> logoutRedirectUri;
    private boolean enabled;
    private boolean invalidated;
    private Set<String> scopes;
    @JsonProperty("created_on")
    private Timestamp createdOn;
    @JsonProperty("created_by")
    private String createdBy;
    @JsonProperty("modified_on")
    private Timestamp modifiedOn;
    @JsonProperty("modified_by")
    private String modifiedBy;
}