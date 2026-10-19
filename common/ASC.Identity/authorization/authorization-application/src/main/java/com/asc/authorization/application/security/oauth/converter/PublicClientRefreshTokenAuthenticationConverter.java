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
import java.util.HashMap;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.web.authentication.AuthenticationConverter;
import org.springframework.security.web.util.matcher.RequestMatcher;
import org.springframework.util.StringUtils;

/**
 * Converts a refresh token request from a public client (PKCE, no client secret) into an {@link
 * OAuth2ClientAuthenticationToken} with {@link ClientAuthenticationMethod#NONE}.
 *
 * <p>Spring's {@code PublicClientAuthenticationConverter} only recognises authorization code
 * requests that carry a {@code code_verifier}, so a public client could never authenticate a {@code
 * refresh_token} grant. Requests with credentials are left to the default converters.
 *
 * @see
 *     com.asc.authorization.application.security.oauth.provider.PublicClientRefreshTokenAuthenticationProvider
 */
public class PublicClientRefreshTokenAuthenticationConverter implements AuthenticationConverter {
  private final RequestMatcher revocationEndpointMatcher;

  /**
   * Constructs a new {@code PublicClientRefreshTokenAuthenticationConverter}.
   *
   * @param revocationEndpointMatcher matches requests to the token revocation endpoint
   */
  public PublicClientRefreshTokenAuthenticationConverter(RequestMatcher revocationEndpointMatcher) {
    this.revocationEndpointMatcher = revocationEndpointMatcher;
  }

  /**
   * Converts the request, or returns {@code null} when it is not a public client refresh or
   * revocation request.
   *
   * @param request the token or token revocation endpoint request
   * @return an unauthenticated {@link OAuth2ClientAuthenticationToken}, or {@code null}
   * @throws OAuth2AuthenticationException {@code invalid_request} when {@code client_id} is
   *     repeated or empty
   */
  @Override
  public Authentication convert(HttpServletRequest request) {
    if (!HttpMethod.POST.matches(request.getMethod())
        || request.getHeader(HttpHeaders.AUTHORIZATION) != null) return null;

    var parameters = FormParameters.from(request);
    if (!(AuthorizationGrantType.REFRESH_TOKEN
            .getValue()
            .equals(parameters.getFirst(OAuth2ParameterNames.GRANT_TYPE))
        || revocationEndpointMatcher.matches(request))) return null;

    if (parameters.containsKey(OAuth2ParameterNames.CLIENT_SECRET)
        || parameters.containsKey(OAuth2ParameterNames.CLIENT_ASSERTION)) return null;

    var clientIds = parameters.get(OAuth2ParameterNames.CLIENT_ID);
    if (clientIds == null) return null;

    if (clientIds.size() != 1 || !StringUtils.hasText(clientIds.getFirst()))
      throw new OAuth2AuthenticationException(OAuth2ErrorCodes.INVALID_REQUEST);

    var additionalParameters = new HashMap<String, Object>();
    parameters.forEach(
        (key, values) -> {
          if (!OAuth2ParameterNames.CLIENT_ID.equals(key))
            additionalParameters.put(
                key, values.size() == 1 ? values.getFirst() : values.toArray(String[]::new));
        });

    return new OAuth2ClientAuthenticationToken(
        clientIds.getFirst(), ClientAuthenticationMethod.NONE, null, additionalParameters);
  }
}
