/**
 *
 */
package com.onlyoffice.authorization.api.web.server.messaging.messages;

import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;
import java.time.Instant;

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
    private Timestamp authorizationCodeIssuedAt;
    private Timestamp authorizationCodeExpiresAt;
    private String authorizationCodeMetadata;
    private String accessTokenValue;
    private Timestamp accessTokenIssuedAt;
    private Timestamp accessTokenExpiresAt;
    private String accessTokenMetadata;
    private String accessTokenType;
    private String accessTokenScopes;
    private String refreshTokenValue;
    private Timestamp refreshTokenIssuedAt;
    private Timestamp refreshTokenExpiresAt;
    private String refreshTokenMetadata;
    @Builder.Default
    private Timestamp modifiedAt = Timestamp.from(Instant.now());
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