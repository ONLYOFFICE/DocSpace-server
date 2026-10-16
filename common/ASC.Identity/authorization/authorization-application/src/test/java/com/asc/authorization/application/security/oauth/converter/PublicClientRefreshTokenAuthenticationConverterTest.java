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

package com.asc.authorization.application.security.oauth.converter;

import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.Test;
import org.springframework.http.HttpHeaders;
import org.springframework.mock.web.MockHttpServletRequest;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;

public class PublicClientRefreshTokenAuthenticationConverterTest {
  private final PublicClientRefreshTokenAuthenticationConverter converter =
      new PublicClientRefreshTokenAuthenticationConverter();

  private static MockHttpServletRequest refreshRequest() {
    var request = new MockHttpServletRequest("POST", "/oauth2/token");
    request.addParameter("grant_type", "refresh_token");
    request.addParameter("refresh_token", "token");
    request.addParameter("client_id", "client");
    return request;
  }

  @Test
  void whenRefreshRequestHasOnlyClientId_thenPublicClientAuthentication() {
    var authentication = (OAuth2ClientAuthenticationToken) converter.convert(refreshRequest());

    assertNotNull(authentication);
    assertEquals("client", authentication.getPrincipal());
    assertEquals(ClientAuthenticationMethod.NONE, authentication.getClientAuthenticationMethod());
    assertFalse(authentication.isAuthenticated());
    assertEquals("refresh_token", authentication.getAdditionalParameters().get("grant_type"));
    assertEquals("token", authentication.getAdditionalParameters().get("refresh_token"));
    assertFalse(authentication.getAdditionalParameters().containsKey("client_id"));
  }

  @Test
  void whenRefreshRequestHasClientSecret_thenNotConverted() {
    var request = refreshRequest();
    request.addParameter("client_secret", "secret");

    assertNull(converter.convert(request));
  }

  @Test
  void whenRefreshRequestHasAuthorizationHeader_thenNotConverted() {
    var request = refreshRequest();
    request.addHeader(HttpHeaders.AUTHORIZATION, "Basic Y2xpZW50OnNlY3JldA==");

    assertNull(converter.convert(request));
  }

  @Test
  void whenRefreshRequestHasClientAssertion_thenNotConverted() {
    var request = refreshRequest();
    request.addParameter("client_assertion", "jwt");

    assertNull(converter.convert(request));
  }

  @Test
  void whenGrantIsAuthorizationCode_thenNotConverted() {
    var request = new MockHttpServletRequest("POST", "/oauth2/token");
    request.addParameter("grant_type", "authorization_code");
    request.addParameter("client_id", "client");
    request.addParameter("code_verifier", "verifier");

    assertNull(converter.convert(request));
  }

  @Test
  void whenRequestIsGet_thenNotConverted() {
    var request = refreshRequest();
    request.setMethod("GET");

    assertNull(converter.convert(request));
  }

  @Test
  void whenClientIdIsMissing_thenNotConverted() {
    var request = new MockHttpServletRequest("POST", "/oauth2/token");
    request.addParameter("grant_type", "refresh_token");
    request.addParameter("refresh_token", "token");

    assertNull(converter.convert(request));
  }

  @Test
  void whenClientIdIsRepeated_thenInvalidRequest() {
    var request = refreshRequest();
    request.addParameter("client_id", "other");

    var exception =
        assertThrows(OAuth2AuthenticationException.class, () -> converter.convert(request));
    assertEquals(OAuth2ErrorCodes.INVALID_REQUEST, exception.getError().getErrorCode());
  }
}
