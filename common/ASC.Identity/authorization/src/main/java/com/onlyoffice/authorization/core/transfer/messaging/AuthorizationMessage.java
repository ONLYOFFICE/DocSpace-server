/**
 *
 */
package com.onlyoffice.authorization.core.transfer.messaging;

import lombok.*;

import java.io.Serializable;
import java.time.ZonedDateTime;

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
    private ZonedDateTime modifiedAt;
    private Boolean invalidated;
}