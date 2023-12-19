/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.messages;

import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Set;

/**
 *
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientMessage implements Message, Serializable {
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
    private String allowedOrigins;
    private String logoutRedirectUri;
    private Set<String> scopes;
    private int tenant;
    private String tenantUrl;
    private boolean enabled;
    private boolean invalidated;
    private ZonedDateTime createdOn;
    private String createdBy;
    @Builder.Default
    private ZonedDateTime modifiedOn = ZonedDateTime.now();
    private String modifiedBy;
    @Builder.Default
    private ClientCommandCode commandCode = ClientCommandCode.CREATE_CLIENT;
    public String getCode() {
        return commandCode.name();
    }
    public enum ClientCommandCode {
        CREATE_CLIENT,
        UPDATE_CLIENT
    }
}
