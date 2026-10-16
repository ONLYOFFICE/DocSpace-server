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

package com.asc.authorization.application.security.oauth.provider;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;

public class PublicClientRefreshTokenAuthenticationProviderTest {
  private RegisteredClientRepository repository;
  private PublicClientRefreshTokenAuthenticationProvider provider;

  private static RegisteredClient client(String id, ClientAuthenticationMethod... methods) {
    var builder =
        RegisteredClient.withId(id)
            .clientId(id)
            .clientSecret("secret")
            .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
            .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
            .redirectUri("https://example.com/callback");
    for (var method : methods) builder.clientAuthenticationMethod(method);
    return builder.build();
  }

  private static OAuth2ClientAuthenticationToken publicAuthentication(
      String clientId, String grantType) {
    return new OAuth2ClientAuthenticationToken(
        clientId,
        ClientAuthenticationMethod.NONE,
        null,
        Map.of("grant_type", grantType, "refresh_token", "token"));
  }

  @BeforeEach
  void setUp() {
    repository = mock(RegisteredClientRepository.class);
    when(repository.findByClientId("pkce"))
        .thenReturn(
            client(
                "pkce",
                ClientAuthenticationMethod.CLIENT_SECRET_POST,
                ClientAuthenticationMethod.NONE));
    when(repository.findByClientId("confidential"))
        .thenReturn(client("confidential", ClientAuthenticationMethod.CLIENT_SECRET_POST));
    provider = new PublicClientRefreshTokenAuthenticationProvider(repository);
  }

  @Test
  void whenPkceClientRefreshes_thenAuthenticated() {
    var result =
        (OAuth2ClientAuthenticationToken)
            provider.authenticate(publicAuthentication("pkce", "refresh_token"));

    assertNotNull(result);
    assertTrue(result.isAuthenticated());
    assertEquals("pkce", result.getRegisteredClient().getClientId());
    assertEquals(ClientAuthenticationMethod.NONE, result.getClientAuthenticationMethod());
  }

  @Test
  void whenClientDoesNotAllowPkce_thenInvalidClient() {
    var exception =
        assertThrows(
            OAuth2AuthenticationException.class,
            () -> provider.authenticate(publicAuthentication("confidential", "refresh_token")));
    assertEquals(OAuth2ErrorCodes.INVALID_CLIENT, exception.getError().getErrorCode());
  }

  @Test
  void whenClientIsUnknown_thenInvalidClient() {
    var exception =
        assertThrows(
            OAuth2AuthenticationException.class,
            () -> provider.authenticate(publicAuthentication("unknown", "refresh_token")));
    assertEquals(OAuth2ErrorCodes.INVALID_CLIENT, exception.getError().getErrorCode());
  }

  @Test
  void whenGrantIsAuthorizationCode_thenLeftToDefaultProviders() {
    assertNull(provider.authenticate(publicAuthentication("pkce", "authorization_code")));
    verifyNoInteractions(repository);
  }

  @Test
  void whenClientUsesSecret_thenLeftToDefaultProviders() {
    var authentication =
        new OAuth2ClientAuthenticationToken(
            "pkce",
            ClientAuthenticationMethod.CLIENT_SECRET_POST,
            "secret",
            Map.of("grant_type", "refresh_token"));

    assertNull(provider.authenticate(authentication));
    verifyNoInteractions(repository);
  }

  @Test
  void whenSupportsChecked_thenClientAuthenticationTokensSupported() {
    assertTrue(provider.supports(OAuth2ClientAuthenticationToken.class));
  }
}
