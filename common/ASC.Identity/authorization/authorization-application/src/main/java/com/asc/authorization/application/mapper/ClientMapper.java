// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.authorization.application.mapper;

import com.asc.authorization.application.configuration.properties.RegisteredClientConfigurationProperties;
import com.asc.authorization.application.security.oauth.grant.ExtendedAuthorizationGrantType;
import com.asc.common.service.transfer.response.ClientResponse;
import java.time.Duration;
import java.time.Instant;
import java.util.HashSet;
import lombok.RequiredArgsConstructor;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.oauth2.server.authorization.settings.TokenSettings;
import org.springframework.stereotype.Component;

/** Mapper class for converting between {@link ClientResponse}, and {@link RegisteredClient}. */
@Component
@RequiredArgsConstructor
public class ClientMapper {
  private final RegisteredClientConfigurationProperties configuration;

  /**
   * Converts a {@link ClientResponse} to a {@link RegisteredClient}.
   *
   * @param clientResponse the {@link ClientResponse} to convert.
   * @return the converted {@link RegisteredClient}.
   */
  public RegisteredClient toRegisteredClient(ClientResponse clientResponse) {
    return RegisteredClient.withId(clientResponse.getClientId())
        .clientId(clientResponse.getClientId())
        .clientIdIssuedAt(clientResponse.getCreatedOn().toInstant())
        .clientSecret(clientResponse.getClientSecret())
        .clientName(clientResponse.getName())
        .clientAuthenticationMethods(
            methods -> {
              var clientAuthenticationMethod =
                  new HashSet<>(clientResponse.getAuthenticationMethods());
              for (String method : clientAuthenticationMethod) {
                methods.add(new ClientAuthenticationMethod(method));
              }
            })
        .authorizationGrantType(ExtendedAuthorizationGrantType.PERSONAL_ACCESS_TOKEN)
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
        .redirectUris(uris -> uris.addAll(clientResponse.getRedirectUris()))
        .scopes(scopes -> scopes.addAll(clientResponse.getScopes()))
        .clientSettings(
            ClientSettings.builder()
                .requireProofKey(false)
                .requireAuthorizationConsent(true)
                .build())
        .tokenSettings(
            TokenSettings.builder()
                .accessTokenTimeToLive(Duration.ofMinutes(configuration.getAccessTokenMinutesTTL()))
                .refreshTokenTimeToLive(Duration.ofDays(configuration.getRefreshTokenDaysTTL()))
                .authorizationCodeTimeToLive(
                    Duration.ofMinutes(configuration.getAuthorizationCodeMinutesTTL()))
                .reuseRefreshTokens(false)
                .build())
        .build();
  }

  /**
   * Converts a {@link com.asc.common.application.proto.ClientResponse} to a {@link
   * RegisteredClient}.
   *
   * @param clientResponse the {@link com.asc.common.application.proto.ClientResponse} to convert.
   * @return the converted {@link RegisteredClient}.
   */
  public RegisteredClient toRegisteredClient(
      com.asc.common.application.proto.ClientResponse clientResponse) {
    return RegisteredClient.withId(clientResponse.getClientId())
        .clientId(clientResponse.getClientId())
        .clientIdIssuedAt(
            Instant.ofEpochSecond(
                clientResponse.getCreatedOn().getSeconds(),
                clientResponse.getCreatedOn().getNanos()))
        .clientSecret(clientResponse.getClientSecret())
        .clientName(clientResponse.getName())
        .clientAuthenticationMethods(
            methods -> {
              var clientAuthenticationMethod =
                  new HashSet<>(clientResponse.getAuthenticationMethodsList());
              for (String method : clientAuthenticationMethod) {
                methods.add(new ClientAuthenticationMethod(method));
              }
            })
        .authorizationGrantType(ExtendedAuthorizationGrantType.PERSONAL_ACCESS_TOKEN)
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
        .redirectUris(uris -> uris.addAll(clientResponse.getRedirectUrisList()))
        .scopes(scopes -> scopes.addAll(clientResponse.getScopesList()))
        .clientSettings(
            ClientSettings.builder()
                .requireProofKey(false)
                .requireAuthorizationConsent(true)
                .build())
        .tokenSettings(
            TokenSettings.builder()
                .accessTokenTimeToLive(Duration.ofMinutes(configuration.getAccessTokenMinutesTTL()))
                .refreshTokenTimeToLive(Duration.ofDays(configuration.getRefreshTokenDaysTTL()))
                .authorizationCodeTimeToLive(
                    Duration.ofMinutes(configuration.getAuthorizationCodeMinutesTTL()))
                .reuseRefreshTokens(false)
                .build())
        .build();
  }
}
