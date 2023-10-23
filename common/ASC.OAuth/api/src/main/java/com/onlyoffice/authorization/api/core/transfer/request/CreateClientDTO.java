/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.onlyoffice.authorization.api.external.validation.annotations.URLCollection;
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
    @Size(min = 10, max = 256, message = "client description length is expected to be between 10 and 256 characters")
    private String description;
    @JsonProperty("logo")
    @Pattern(regexp = "^data:((?:\\w+\\/(?:(?!;).)+)?)((?:;[\\w\\W]*?[^;])*),(.+)$",
    message = "client logo is expected to be passed as base64")
    private String logo;
    @JsonProperty("authentication_method")
    private String authenticationMethod;
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
    private Set<String> allowed_origins;
    @JsonProperty("logout_redirect_uri")
    @Pattern(regexp = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
    message = "logout redirect uri is expected to be passed as url")
    private String logoutRedirectUri;
    @NotEmpty(message = "scopes field can not be empty")
    private Set<String> scopes;
}
