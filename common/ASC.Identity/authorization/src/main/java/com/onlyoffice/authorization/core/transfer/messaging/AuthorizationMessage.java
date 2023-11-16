/**
 *
 */
package com.onlyoffice.authorization.core.transfer.messaging;

import lombok.*;

import java.io.Serializable;
import java.sql.Timestamp;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class AuthorizationMessage implements Serializable {
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
    private Timestamp modifiedAt;
    private Boolean invalidated;
}