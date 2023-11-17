/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.messages;

import lombok.*;

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
public class ClientMessage implements Serializable {
    private String name;
    private String clientId;
    private String clientSecret;
    private String description;
    private String websiteUrl;
    private String termsUrl;
    private String policyUrl;
    private String logo;
    private String authenticationMethod;
    private String redirectUris;
    private String logoutRedirectUri;
    private Set<String> scopes;
    private int tenant;
    private String tenantUrl;
    private boolean enabled;
    private boolean invalidated;
    private Timestamp createdOn;
    private String createdBy;
    private Timestamp modifiedOn;
    private String modifiedBy;
}
