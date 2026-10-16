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

import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;

/**
 * Authenticates public clients (PKCE, no client secret) on the {@code refresh_token} grant.
 *
 * <p>Spring's {@code PublicClientAuthenticationProvider} requires a {@code code_verifier}, which
 * only an authorization code request has, so it rejects every refresh request from a public client.
 * This provider accepts a {@link ClientAuthenticationMethod#NONE} authentication for the {@code
 * refresh_token} grant when the registered client allows that method (PKCE enabled). The refresh
 * token itself is then validated by {@code OAuth2RefreshTokenAuthenticationProvider}: it must be
 * active and issued to this client. Refresh tokens are rotated on every use ({@code
 * reuseRefreshTokens(false)}), as OAuth 2.0 Security BCP requires for public clients.
 *
 * <p>Any other authentication, including {@code NONE} on the authorization code grant, is left to
 * the default providers.
 */
public class PublicClientRefreshTokenAuthenticationProvider implements AuthenticationProvider {
  private final RegisteredClientRepository registeredClientRepository;

  /**
   * Constructs a new {@code PublicClientRefreshTokenAuthenticationProvider}.
   *
   * @param registeredClientRepository the repository used to look up the client
   */
  public PublicClientRefreshTokenAuthenticationProvider(
      RegisteredClientRepository registeredClientRepository) {
    this.registeredClientRepository = registeredClientRepository;
  }

  private static OAuth2AuthenticationException invalidClient(String parameterName) {
    return new OAuth2AuthenticationException(
        new OAuth2Error(
            OAuth2ErrorCodes.INVALID_CLIENT,
            "Client authentication failed: " + parameterName,
            null));
  }

  /**
   * Authenticates a public client refresh request.
   *
   * @param authentication the {@link OAuth2ClientAuthenticationToken} to authenticate
   * @return the authenticated client, or {@code null} when this provider does not apply
   * @throws AuthenticationException {@code invalid_client} when the client is unknown or does not
   *     allow authentication without a secret
   */
  @Override
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    var clientAuthentication = (OAuth2ClientAuthenticationToken) authentication;
    if (!ClientAuthenticationMethod.NONE.equals(
            clientAuthentication.getClientAuthenticationMethod())
        || !AuthorizationGrantType.REFRESH_TOKEN
            .getValue()
            .equals(
                clientAuthentication
                    .getAdditionalParameters()
                    .get(OAuth2ParameterNames.GRANT_TYPE))) return null;

    var clientId = clientAuthentication.getPrincipal();
    if (clientId == null || clientId.toString().isBlank())
      throw invalidClient(OAuth2ParameterNames.CLIENT_ID);

    var registeredClient = registeredClientRepository.findByClientId(clientId.toString());
    if (registeredClient == null) throw invalidClient(OAuth2ParameterNames.CLIENT_ID);

    if (!registeredClient
        .getClientAuthenticationMethods()
        .contains(ClientAuthenticationMethod.NONE)) throw invalidClient("authentication_method");

    return new OAuth2ClientAuthenticationToken(
        registeredClient, ClientAuthenticationMethod.NONE, null);
  }

  /**
   * Supports client authentication tokens.
   *
   * @param authentication the authentication class
   * @return {@code true} for {@link OAuth2ClientAuthenticationToken}
   */
  @Override
  public boolean supports(Class<?> authentication) {
    return OAuth2ClientAuthenticationToken.class.isAssignableFrom(authentication);
  }
}
