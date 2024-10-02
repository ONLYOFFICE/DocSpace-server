// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.application.security.oauth.providers;

import java.net.URL;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.convert.TypeDescriptor;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.core.OAuth2TokenIntrospectionClaimNames;
import org.springframework.security.oauth2.core.converter.ClaimConversionService;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenIntrospection;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2TokenIntrospectionAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Component;
import org.springframework.util.CollectionUtils;

/**
 * Provides authentication for OAuth2 token introspection requests.
 *
 * <p>This class implements {@link AuthenticationProvider} to authenticate token introspection
 * requests by validating the provided token against the stored authorizations and registered
 * clients.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscTokenIntrospectionAuthenticationProvider implements AuthenticationProvider {
  private static final TypeDescriptor OBJECT_TYPE_DESCRIPTOR = TypeDescriptor.valueOf(Object.class);

  private static final TypeDescriptor LIST_STRING_TYPE_DESCRIPTOR =
      TypeDescriptor.collection(List.class, TypeDescriptor.valueOf(String.class));

  private final RegisteredClientRepository registeredClientRepository;

  private final OAuth2AuthorizationService authorizationService;

  /**
   * Authenticates the provided token introspection request.
   *
   * @param authentication the authentication request object, which should be an instance of {@link
   *     OAuth2TokenIntrospectionAuthenticationToken}
   * @return a fully authenticated token introspection request or the original request if
   *     authentication fails
   * @throws AuthenticationException if an authentication error occurs
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    var tokenIntrospectionAuthentication =
        (OAuth2TokenIntrospectionAuthenticationToken) authentication;

    var authorization =
        authorizationService.findByToken(tokenIntrospectionAuthentication.getToken(), null);
    if (authorization == null) {
      log.debug("Did not authenticate token introspection request since token was not found");
      // Return the authentication request when token not found
      return tokenIntrospectionAuthentication;
    }

    log.trace("Retrieved authorization with token");

    var authorizedToken = authorization.getToken(tokenIntrospectionAuthentication.getToken());
    if (!authorizedToken.isActive()) {
      log.trace("Did not introspect token since not active");
      return new OAuth2TokenIntrospectionAuthenticationToken(
          tokenIntrospectionAuthentication.getToken(),
          authentication,
          OAuth2TokenIntrospection.builder().build());
    }

    var authorizedClient =
        registeredClientRepository.findById(authorization.getRegisteredClientId());
    var tokenClaims = withActiveTokenClaims(authorizedToken, authorizedClient);

    log.trace("Authenticated token introspection request");

    return new OAuth2TokenIntrospectionAuthenticationToken(
        authorizedToken.getToken().getTokenValue(), authentication, tokenClaims);
  }

  /**
   * Indicates whether this {@code AuthenticationProvider} supports the specified authentication
   * type.
   *
   * @param authentication the class of the authentication request object
   * @return {@code true} if the authentication type is supported, otherwise {@code false}
   */
  public boolean supports(Class<?> authentication) {
    return OAuth2TokenIntrospectionAuthenticationToken.class.isAssignableFrom(authentication);
  }

  /**
   * Constructs an {@link OAuth2TokenIntrospection} object with the active token claims.
   *
   * @param authorizedToken the authorized token
   * @param authorizedClient the registered client
   * @return the constructed {@code OAuth2TokenIntrospection} object
   */
  private static OAuth2TokenIntrospection withActiveTokenClaims(
      OAuth2Authorization.Token<OAuth2Token> authorizedToken, RegisteredClient authorizedClient) {

    OAuth2TokenIntrospection.Builder tokenClaims;
    if (!CollectionUtils.isEmpty(authorizedToken.getClaims())) {
      Map<String, Object> claims = convertClaimsIfNecessary(authorizedToken.getClaims());
      tokenClaims = OAuth2TokenIntrospection.withClaims(claims).active(true);
    } else {
      tokenClaims = OAuth2TokenIntrospection.builder(true);
    }

    tokenClaims.clientId(authorizedClient.getClientId());

    var token = authorizedToken.getToken();
    if (token.getIssuedAt() != null) tokenClaims.issuedAt(token.getIssuedAt());
    if (token.getExpiresAt() != null) tokenClaims.expiresAt(token.getExpiresAt());

    if (OAuth2AccessToken.class.isAssignableFrom(token.getClass())) {
      var accessToken = (OAuth2AccessToken) token;
      tokenClaims.tokenType(accessToken.getTokenType().getValue());
    }

    return tokenClaims.build();
  }

  /**
   * Converts token claims if necessary.
   *
   * @param claims the original claims
   * @return the converted claims
   */
  private static Map<String, Object> convertClaimsIfNecessary(Map<String, Object> claims) {
    var convertedClaims = new HashMap<String, Object>(claims);

    var value = claims.get(OAuth2TokenIntrospectionClaimNames.ISS);
    if (value != null && !(value instanceof URL)) {
      var convertedValue = ClaimConversionService.getSharedInstance().convert(value, URL.class);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.ISS, convertedValue);
    }

    value = claims.get(OAuth2TokenIntrospectionClaimNames.SCOPE);
    if (value != null && !(value instanceof List)) {
      var convertedValue =
          ClaimConversionService.getSharedInstance()
              .convert(value, OBJECT_TYPE_DESCRIPTOR, LIST_STRING_TYPE_DESCRIPTOR);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.SCOPE, convertedValue);
    }

    value = claims.get(OAuth2TokenIntrospectionClaimNames.AUD);
    if (value != null && !(value instanceof List)) {
      var convertedValue =
          ClaimConversionService.getSharedInstance()
              .convert(value, OBJECT_TYPE_DESCRIPTOR, LIST_STRING_TYPE_DESCRIPTOR);
      if (convertedValue != null)
        convertedClaims.put(OAuth2TokenIntrospectionClaimNames.AUD, convertedValue);
    }

    return convertedClaims;
  }
}
