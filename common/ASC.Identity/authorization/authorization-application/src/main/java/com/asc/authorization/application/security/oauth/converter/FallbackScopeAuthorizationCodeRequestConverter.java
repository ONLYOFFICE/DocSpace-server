// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.authorization.application.security.oauth.converter;

import jakarta.servlet.http.HttpServletRequest;
import java.util.HashSet;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.security.oauth2.server.authorization.web.authentication.OAuth2AuthorizationCodeRequestAuthenticationConverter;
import org.springframework.security.web.authentication.AuthenticationConverter;
import org.springframework.stereotype.Component;

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
@Component
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

    var registeredClient =
        registeredClientRepository.findByClientId(
            authorizationCodeRequestAuthentication.getClientId());

    if (registeredClient == null) return authorizationCodeRequestAuthentication;

    var requestedScopes = new HashSet<>(authorizationCodeRequestAuthentication.getScopes());
    if (requestedScopes.isEmpty()) requestedScopes.addAll(registeredClient.getScopes());

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
