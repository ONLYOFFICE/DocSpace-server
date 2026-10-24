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

package com.asc.authorization.application.security.oauth.generator;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

import java.util.Objects;
import org.junit.jupiter.api.Test;
import org.springframework.core.env.Environment;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.token.DefaultOAuth2TokenContext;

public class PrefixedRefreshTokenGeneratorTest {
  private static final RegisteredClient CLIENT =
      RegisteredClient.withId("client")
          .clientId("client")
          .clientAuthenticationMethod(ClientAuthenticationMethod.NONE)
          .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
          .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
          .redirectUri("http://localhost:17890/oauth/callback")
          .build();

  private static Environment environment(String... profiles) {
    var environment = mock(Environment.class);
    when(environment.getActiveProfiles()).thenReturn(profiles);
    return environment;
  }

  private static DefaultOAuth2TokenContext.Builder publicCodeExchange(
      OAuth2Authorization authorization) {
    var clientPrincipal =
        new OAuth2ClientAuthenticationToken(CLIENT, ClientAuthenticationMethod.NONE, null);
    return DefaultOAuth2TokenContext.builder()
        .registeredClient(CLIENT)
        .authorization(authorization)
        .tokenType(OAuth2TokenType.REFRESH_TOKEN)
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrant(
            new OAuth2AuthorizationCodeAuthenticationToken(
                "code", clientPrincipal, "http://localhost:17890/oauth/callback", null));
  }

  private static OAuth2Authorization authorization(String id) {
    return OAuth2Authorization.withRegisteredClient(CLIENT)
        .id(id)
        .principalName("user")
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .build();
  }

  @Test
  void whenPublicClientExchangesCode_thenRefreshTokenIssued() {
    var generator = new PrefixedRefreshTokenGenerator(environment(), "eu");
    var token = generator.generate(publicCodeExchange(authorization("auth-1")).build());

    assertNotNull(token);
    assertTrue(token.getTokenValue().matches("[A-Za-z0-9_-]{128}"));
    assertNotNull(token.getExpiresAt());
  }

  @Test
  void whenSaasProfile_thenRegionPrefixIsAdded() {
    var generator = new PrefixedRefreshTokenGenerator(environment("saas"), "EU");
    var token = generator.generate(publicCodeExchange(authorization("auth-2")).build());

    assertNotNull(token);
    assertTrue(token.getTokenValue().matches("eu:[A-Za-z0-9_-]{128}"));
  }

  @Test
  void whenTokenTypeIsNotRefreshToken_thenNothingGenerated() {
    var generator = new PrefixedRefreshTokenGenerator(environment(), "");

    assertNull(
        generator.generate(
            publicCodeExchange(authorization("auth-3"))
                .tokenType(OAuth2TokenType.ACCESS_TOKEN)
                .build()));
  }

  @Test
  void whenTokensAreGenerated_thenTheyAreUnique() {
    var generator = new PrefixedRefreshTokenGenerator(environment(), "");
    var context = publicCodeExchange(authorization("auth-4")).build();

    assertNotEquals(
        Objects.requireNonNull(generator.generate(context)).getTokenValue(),
        Objects.requireNonNull(generator.generate(context)).getTokenValue());
  }
}
