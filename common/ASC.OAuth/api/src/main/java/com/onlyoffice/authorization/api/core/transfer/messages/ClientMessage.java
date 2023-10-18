/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.messages;

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
public class ClientMessage implements Serializable {
    private String name;
    private String clientId;
    private String clientSecret;
    private String description;
    private String termsUrl;
    private String policyUrl;
    private String logoUrl;
    private String authenticationMethod;
    private String redirectUri;
    private String logoutRedirectUri;
    private Set<String> scopes;
    private int tenant;
    private boolean enabled;
    private boolean invalidated;
}
