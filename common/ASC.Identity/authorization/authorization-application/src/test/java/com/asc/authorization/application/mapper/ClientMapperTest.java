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

import static org.junit.jupiter.api.Assertions.*;

import com.asc.authorization.application.configuration.properties.RegisteredClientConfigurationProperties;
import com.asc.authorization.application.security.oauth.grant.ExtendedAuthorizationGrantType;
import com.asc.common.service.transfer.response.ClientResponse;
import java.time.Duration;
import java.time.ZonedDateTime;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;

public class ClientMapperTest {
  private RegisteredClientConfigurationProperties configuration;
  private ClientMapper clientMapper;

  @BeforeEach
  void setUp() {
    configuration = new RegisteredClientConfigurationProperties();
    configuration.setAccessTokenMinutesTTL(60);
    configuration.setRefreshTokenDaysTTL(30);
    configuration.setAuthorizationCodeMinutesTTL(1);
    clientMapper = new ClientMapper(configuration);
  }

  @Test
  void whenClientResponseIsMappedToRegisteredClient_thenRegisteredClientIsCreated() {
    var now = ZonedDateTime.now();
    var clientResponse =
        ClientResponse.builder()
            .clientId("client")
            .clientSecret("secret")
            .name("Test")
            .authenticationMethods(Set.of("client_secret_basic", "client_secret_post"))
            .redirectUris(Set.of("https://mock.com/callback"))
            .scopes(Set.of("read", "write", "openid"))
            .createdOn(now)
            .build();

    var registeredClient = clientMapper.toRegisteredClient(clientResponse);

    assertEquals("client", registeredClient.getId());
    assertEquals("client", registeredClient.getClientId());
    assertEquals("secret", registeredClient.getClientSecret());
    assertEquals("Test", registeredClient.getClientName());
    assertEquals(now.toInstant(), registeredClient.getClientIdIssuedAt());
  }

  @Test
  void whenClientResponseIsMapped_thenAuthenticationMethodsAreSet() {
    var clientResponse =
        ClientResponse.builder()
            .clientId("client")
            .clientSecret("secret")
            .name("Test")
            .authenticationMethods(Set.of("client_secret_basic"))
            .redirectUris(Set.of("https://mock.com/callback"))
            .scopes(Set.of("read"))
            .createdOn(ZonedDateTime.now())
            .build();

    var registeredClient = clientMapper.toRegisteredClient(clientResponse);

    assertTrue(
        registeredClient
            .getClientAuthenticationMethods()
            .contains(new ClientAuthenticationMethod("client_secret_basic")));
  }

  @Test
  void whenClientResponseIsMapped_thenGrantTypesAreSet() {
    var clientResponse =
        ClientResponse.builder()
            .clientId("client")
            .clientSecret("secret")
            .name("Test")
            .authenticationMethods(Set.of("client_secret_basic"))
            .redirectUris(Set.of("https://mock.com/callback"))
            .scopes(Set.of("read"))
            .createdOn(ZonedDateTime.now())
            .build();

    var registeredClient = clientMapper.toRegisteredClient(clientResponse);

    assertTrue(
        registeredClient
            .getAuthorizationGrantTypes()
            .contains(AuthorizationGrantType.AUTHORIZATION_CODE));
    assertTrue(
        registeredClient
            .getAuthorizationGrantTypes()
            .contains(AuthorizationGrantType.REFRESH_TOKEN));
    assertTrue(
        registeredClient
            .getAuthorizationGrantTypes()
            .contains(ExtendedAuthorizationGrantType.PERSONAL_ACCESS_TOKEN));
  }

  @Test
  void whenClientResponseIsMapped_thenScopesAreSet() {
    var scopes = Set.of("read", "write", "openid", "profile");
    var clientResponse =
        ClientResponse.builder()
            .clientId("client")
            .clientSecret("secret")
            .name("Test")
            .authenticationMethods(Set.of("client_secret_basic"))
            .redirectUris(Set.of("https://mock.com/callback"))
            .scopes(scopes)
            .createdOn(ZonedDateTime.now())
            .build();

    var registeredClient = clientMapper.toRegisteredClient(clientResponse);

    assertEquals(scopes, registeredClient.getScopes());
  }

  @Test
  void whenClientResponseIsMapped_thenTokenSettingsAreConfigured() {
    var clientResponse =
        ClientResponse.builder()
            .clientId("client")
            .clientSecret("secret")
            .name("Test")
            .authenticationMethods(Set.of("client_secret_basic"))
            .redirectUris(Set.of("https://mock.com/callback"))
            .scopes(Set.of("read"))
            .createdOn(ZonedDateTime.now())
            .build();

    var registeredClient = clientMapper.toRegisteredClient(clientResponse);

    assertEquals(
        Duration.ofMinutes(configuration.getAccessTokenMinutesTTL()),
        registeredClient.getTokenSettings().getAccessTokenTimeToLive());
    assertEquals(
        Duration.ofDays(configuration.getRefreshTokenDaysTTL()),
        registeredClient.getTokenSettings().getRefreshTokenTimeToLive());
    assertEquals(
        Duration.ofMinutes(configuration.getAuthorizationCodeMinutesTTL()),
        registeredClient.getTokenSettings().getAuthorizationCodeTimeToLive());
    assertFalse(registeredClient.getTokenSettings().isReuseRefreshTokens());
  }
}
