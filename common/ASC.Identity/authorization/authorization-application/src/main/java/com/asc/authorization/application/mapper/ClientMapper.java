// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
