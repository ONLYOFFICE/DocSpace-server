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

import jakarta.servlet.http.HttpServletRequest;
import java.util.HashSet;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.security.oauth2.server.authorization.web.authentication.OAuth2AuthorizationCodeRequestAuthenticationConverter;
import org.springframework.security.web.authentication.AuthenticationConverter;

/**
 * A custom implementation of {@link AuthenticationConverter} that enhances the standard OAuth2
 * authorization code request authentication process by providing fallback scopes.
 *
 * <p>This converter delegates the initial conversion to the standard {@link
 * OAuth2AuthorizationCodeRequestAuthenticationConverter}, and then enhances the resulting
 * authentication token by adding the registered client's scopes when no scopes are explicitly
 * requested in the original authorization request.
 *
 * <p>This behavior ensures that authorization requests without explicitly defined scopes will still
 * have access to all scopes registered for the client, rather than failing due to empty scopes or
 * defaulting to a restricted set of permissions.
 *
 * @see AuthenticationConverter
 * @see OAuth2AuthorizationCodeRequestAuthenticationConverter
 * @see OAuth2AuthorizationCodeRequestAuthenticationToken
 */
public class FallbackScopeAuthorizationCodeRequestConverter implements AuthenticationConverter {
  private final RegisteredClientRepository registeredClientRepository;
  private final OAuth2AuthorizationCodeRequestAuthenticationConverter delegate =
      new OAuth2AuthorizationCodeRequestAuthenticationConverter();

  /**
   * Constructs a new {@code FallbackScopeAuthorizationCodeRequestConverter} with the specified
   * client repository.
   *
   * @param registeredClientRepository The repository used to retrieve registered client information
   *     for scope resolution. Cannot be null.
   */
  public FallbackScopeAuthorizationCodeRequestConverter(
      RegisteredClientRepository registeredClientRepository) {
    this.registeredClientRepository = registeredClientRepository;
  }

  /**
   * Converts an HTTP request to an OAuth2 authorization code request authentication token.
   *
   * <p>This method extends the standard conversion process by providing fallback scope resolution.
   * If the original authentication request contains no scopes, this converter will populate the
   * scopes from the registered client's configured scopes.
   *
   * <p>The method handles both the initial authorization request and the code redemption request
   * phases of the OAuth2 authorization code flow.
   *
   * @param request The HTTP request to convert. Cannot be null.
   * @return An {@link OAuth2AuthorizationCodeRequestAuthenticationToken} with potentially enhanced
   *     scope information, or {@code null} if the request cannot be converted.
   */
  public Authentication convert(HttpServletRequest request) {
    var authorizationCodeRequestAuthentication =
        (OAuth2AuthorizationCodeRequestAuthenticationToken) delegate.convert(request);

    if (authorizationCodeRequestAuthentication == null) return null;

    var requestedScopes = new HashSet<>(authorizationCodeRequestAuthentication.getScopes());
    if (requestedScopes.isEmpty()) {
      var registeredClient =
          registeredClientRepository.findByClientId(
              authorizationCodeRequestAuthentication.getClientId());
      if (registeredClient == null) return authorizationCodeRequestAuthentication;
      requestedScopes.addAll(registeredClient.getScopes());
    }

    if (authorizationCodeRequestAuthentication.getAuthorizationCode() == null)
      return new OAuth2AuthorizationCodeRequestAuthenticationToken(
          authorizationCodeRequestAuthentication.getAuthorizationUri(),
          authorizationCodeRequestAuthentication.getClientId(),
          (Authentication) authorizationCodeRequestAuthentication.getPrincipal(),
          authorizationCodeRequestAuthentication.getRedirectUri(),
          authorizationCodeRequestAuthentication.getState(),
          requestedScopes,
          authorizationCodeRequestAuthentication.getAdditionalParameters());

    return new OAuth2AuthorizationCodeRequestAuthenticationToken(
        authorizationCodeRequestAuthentication.getAuthorizationUri(),
        authorizationCodeRequestAuthentication.getClientId(),
        (Authentication) authorizationCodeRequestAuthentication.getPrincipal(),
        authorizationCodeRequestAuthentication.getAuthorizationCode(),
        authorizationCodeRequestAuthentication.getRedirectUri(),
        authorizationCodeRequestAuthentication.getState(),
        requestedScopes);
  }
}
