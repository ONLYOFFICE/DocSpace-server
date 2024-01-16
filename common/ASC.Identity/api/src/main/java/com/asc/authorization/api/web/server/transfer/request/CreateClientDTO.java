/**
 *
 */
package com.asc.authorization.api.web.server.transfer.request;

import com.asc.authorization.api.extensions.annotations.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
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
public class CreateClientDTO implements Serializable {
    @NotEmpty
    @Size(min = 3, max = 256, message = "client name length is expected to be between 3 and 256 characters")
    private String name;
    private String description;
    @JsonProperty("logo")
    @Pattern(regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
    message = "client logo is expected to be passed as base64")
    private String logo;
    @JsonProperty("allow_pkce")
    private boolean allowPkce;
    @JsonProperty("website_url")
    @Pattern(regexp = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
            message = "website url is expected to be passed as url")
    private String websiteUrl;
    @JsonProperty("terms_url")
    @Pattern(regexp = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
    message = "terms url is expected to be passed as url")
    private String termsUrl;
    @JsonProperty("policy_url")
    @Pattern(regexp = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
    message = "policy url is expected to be passed as url")
    private String policyUrl;
    @JsonProperty("redirect_uris")
    @URLCollection
    private Set<String> redirectUris;
    @JsonProperty("allowed_origins")
    @URLCollection
    private Set<String> allowedOrigins;
    @JsonProperty("logout_redirect_uri")
    @Pattern(regexp = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
    message = "logout redirect uri is expected to be passed as url")
    private String logoutRedirectUri;
    @NotEmpty(message = "scopes field can not be empty")
    private Set<String> scopes;
}
