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
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;

public class PublicClientRefreshTokenAuthenticationProviderTest {
  private RegisteredClientRepository repository;
  private OAuth2AuthorizationService authorizationService;
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
    return publicAuthentication(clientId, grantType, "public-token");
  }

  private static OAuth2ClientAuthenticationToken publicAuthentication(
      String clientId, String grantType, String refreshToken) {
    return new OAuth2ClientAuthenticationToken(
        clientId,
        ClientAuthenticationMethod.NONE,
        null,
        Map.of("grant_type", grantType, "refresh_token", refreshToken));
  }

  private static OAuth2Authorization authorization(
      RegisteredClient client, ClientAuthenticationMethod issuedWith) {
    var builder =
        OAuth2Authorization.withRegisteredClient(client)
            .principalName("user")
            .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE);
    if (issuedWith != null)
      builder.attribute(
          PublicClientRefreshTokenAuthenticationProvider.CLIENT_AUTHENTICATION_METHOD_ATTRIBUTE,
          issuedWith.getValue());
    return builder.build();
  }

  private void issued(String token, OAuth2Authorization authorization) {
    when(authorizationService.findByToken(token, OAuth2TokenType.REFRESH_TOKEN))
        .thenReturn(authorization);
  }

  private void assertError(String errorCode, OAuth2ClientAuthenticationToken authentication) {
    var exception =
        assertThrows(
            OAuth2AuthenticationException.class, () -> provider.authenticate(authentication));
    assertEquals(errorCode, exception.getError().getErrorCode());
  }

  private static OAuth2ClientAuthenticationToken publicRevocation(String clientId, String token) {
    return new OAuth2ClientAuthenticationToken(
        clientId, ClientAuthenticationMethod.NONE, null, Map.of("token", token));
  }

  private void issuedAny(String token, OAuth2Authorization authorization) {
    when(authorizationService.findByToken(token, null)).thenReturn(authorization);
  }

  @BeforeEach
  void setUp() {
    repository = mock(RegisteredClientRepository.class);
    authorizationService = mock(OAuth2AuthorizationService.class);
    var pkce =
        client(
            "pkce", ClientAuthenticationMethod.CLIENT_SECRET_POST, ClientAuthenticationMethod.NONE);
    when(repository.findByClientId("pkce")).thenReturn(pkce);
    when(repository.findByClientId("other"))
        .thenReturn(
            client(
                "other",
                ClientAuthenticationMethod.CLIENT_SECRET_POST,
                ClientAuthenticationMethod.NONE));
    when(repository.findByClientId("confidential"))
        .thenReturn(client("confidential", ClientAuthenticationMethod.CLIENT_SECRET_POST));
    issued("public-token", authorization(pkce, ClientAuthenticationMethod.NONE));
    issued("secret-token", authorization(pkce, ClientAuthenticationMethod.CLIENT_SECRET_POST));
    issued("legacy-token", authorization(pkce, null));
    provider = new PublicClientRefreshTokenAuthenticationProvider(repository, authorizationService);
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
  void whenPkceClientRevokesPublicToken_thenAuthenticated() {
    issuedAny(
        "public-token",
        authorizationService.findByToken("public-token", OAuth2TokenType.REFRESH_TOKEN));

    var result =
        (OAuth2ClientAuthenticationToken)
            provider.authenticate(publicRevocation("pkce", "public-token"));

    assertTrue(result.isAuthenticated());
    assertEquals(ClientAuthenticationMethod.NONE, result.getClientAuthenticationMethod());
  }

  @Test
  void whenPkceClientRevokesSecretBoundToken_thenInvalidClient() {
    issuedAny(
        "secret-token",
        authorizationService.findByToken("secret-token", OAuth2TokenType.REFRESH_TOKEN));

    assertError(OAuth2ErrorCodes.INVALID_CLIENT, publicRevocation("pkce", "secret-token"));
  }

  @Test
  void whenPkceClientRevokesUnknownToken_thenAuthenticatedSpring() {
    var result =
        (OAuth2ClientAuthenticationToken)
            provider.authenticate(publicRevocation("pkce", "unknown"));

    assertTrue(result.isAuthenticated());
  }

  @Test
  void whenConfidentialClientRevokesWithoutSecret_thenInvalidClient() {
    assertError(OAuth2ErrorCodes.INVALID_CLIENT, publicRevocation("confidential", "public-token"));
  }

  @Test
  void whenTokenWasIssuedWithSecret_thenInvalidClient() {
    assertError(
        OAuth2ErrorCodes.INVALID_CLIENT,
        publicAuthentication("pkce", "refresh_token", "secret-token"));
  }

  @Test
  void whenTokenPredatesTheAttribute_thenInvalidClient() {
    assertError(
        OAuth2ErrorCodes.INVALID_CLIENT,
        publicAuthentication("pkce", "refresh_token", "legacy-token"));
  }

  @Test
  void whenTokenIsUnknown_thenInvalidGrant() {
    assertError(
        OAuth2ErrorCodes.INVALID_GRANT, publicAuthentication("pkce", "refresh_token", "unknown"));
  }

  @Test
  void whenTokenBelongsToAnotherClient_thenInvalidGrant() {
    assertError(
        OAuth2ErrorCodes.INVALID_GRANT,
        publicAuthentication("other", "refresh_token", "public-token"));
  }

  @Test
  void whenRefreshTokenIsMissing_thenInvalidRequest() {
    assertError(
        OAuth2ErrorCodes.INVALID_REQUEST,
        new OAuth2ClientAuthenticationToken(
            "pkce", ClientAuthenticationMethod.NONE, null, Map.of("grant_type", "refresh_token")));
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
    verifyNoInteractions(repository, authorizationService);
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
