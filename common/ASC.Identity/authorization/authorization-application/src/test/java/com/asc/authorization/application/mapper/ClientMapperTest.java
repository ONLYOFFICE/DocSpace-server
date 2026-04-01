// (c) Copyright Ascensio System SIA 2009-2026
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
