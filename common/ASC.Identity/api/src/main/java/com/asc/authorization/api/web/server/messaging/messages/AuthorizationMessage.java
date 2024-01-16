/**
 *
 */
package com.asc.authorization.api.web.server.messaging.messages;

import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;

/**
 *
 */
@Builder
@Getter
@Setter
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class AuthorizationMessage implements Message, Serializable {
    private String id;
    private String registeredClientId;
    private String principalName;
    private String authorizationGrantType;
    private String authorizedScopes;
    private String attributes;
    private String state;
    private String authorizationCodeValue;
    private ZonedDateTime authorizationCodeIssuedAt;
    private ZonedDateTime authorizationCodeExpiresAt;
    private String authorizationCodeMetadata;
    private String accessTokenValue;
    private ZonedDateTime accessTokenIssuedAt;
    private ZonedDateTime accessTokenExpiresAt;
    private String accessTokenMetadata;
    private String accessTokenType;
    private String accessTokenScopes;
    private String refreshTokenValue;
    private ZonedDateTime refreshTokenIssuedAt;
    private ZonedDateTime refreshTokenExpiresAt;
    private String refreshTokenMetadata;
    @Builder.Default
    private ZonedDateTime modifiedAt = ZonedDateTime.now();
    private Boolean invalidated;
    @Builder.Default
    private AuthorizationCommandCode commandCode = AuthorizationCommandCode.CREATE_AUTHORIZATION;
    public String getCode() {
        return commandCode.name();
    }
    public enum AuthorizationCommandCode {
        CREATE_AUTHORIZATION
    }
}