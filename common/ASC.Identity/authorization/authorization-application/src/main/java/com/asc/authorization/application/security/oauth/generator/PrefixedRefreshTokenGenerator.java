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

package com.asc.authorization.application.security.oauth.generator;

import com.asc.authorization.application.security.RegionUtils;
import java.time.Instant;
import java.util.Arrays;
import java.util.Base64;
import org.springframework.core.env.Environment;
import org.springframework.lang.Nullable;
import org.springframework.security.crypto.keygen.Base64StringKeyGenerator;
import org.springframework.security.crypto.keygen.StringKeyGenerator;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2ClientAuthenticationToken;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenGenerator;

/**
 * Custom OAuth2 refresh token generator that optionally prefixes tokens with a region identifier.
 *
 * <p>This generator creates URL-safe Base64 encoded refresh tokens and prepends a region prefix (in
 * the format "region:") for SaaS deployments. The prefix enables cross-region token identification
 * and routing in distributed authorization systems.
 *
 * @see OAuth2TokenGenerator
 * @see OAuth2RefreshToken
 */
public class PrefixedRefreshTokenGenerator implements OAuth2TokenGenerator<OAuth2RefreshToken> {
  /**
   * The key generator that produces cryptographically secure random tokens. Configured to generate
   * 96-byte Base64 URL-encoded strings without padding.
   */
  private final StringKeyGenerator refreshTokenGenerator =
      new Base64StringKeyGenerator(Base64.getUrlEncoder().withoutPadding(), 96);

  /** The Spring environment used to check active profiles. */
  private final Environment environment;

  /**
   * The default region prefix to prepend to refresh tokens. Empty string if no region specified.
   */
  private final String defaultRegion;

  /**
   * Constructs a new PrefixedRefreshTokenGenerator.
   *
   * @param environment the Spring environment, used to determine active profiles
   * @param region the region identifier to prefix tokens with; if null or blank, no prefix is used.
   *     The region is converted to lowercase and appended with a colon separator
   */
  public PrefixedRefreshTokenGenerator(Environment environment, String region) {
    this.environment = environment;
    this.defaultRegion = region != null && !region.isBlank() ? region.toLowerCase() : "";
  }

  /**
   * Extracts the region from the authorization code in the token context.
   *
   * @param context the OAuth2 token context
   * @return the region from the authorization code, or the default region if not found
   */
  private String getRegionFromContext(OAuth2TokenContext context) {
    if (context.getAuthorization() != null) {
      var authCodeToken = context.getAuthorization().getToken(OAuth2AuthorizationCode.class);
      if (authCodeToken != null && authCodeToken.getToken() != null) {
        var region = RegionUtils.extractFromPrefix(authCodeToken.getToken().getTokenValue());
        if (region.isPresent()) {
          return region.get();
        }
      }
    }
    return defaultRegion;
  }

  /**
   * Determines if the OAuth2 token context represents a public client using the authorization code
   * grant.
   *
   * @param context the OAuth2 token context to evaluate
   * @return {@code true} if the context represents a public client using authorization code grant,
   *     {@code false} otherwise
   */
  private static boolean isPublicClientForAuthorizationCodeGrant(OAuth2TokenContext context) {
    if (AuthorizationGrantType.AUTHORIZATION_CODE.equals(context.getAuthorizationGrantType())) {
      var var2 = context.getAuthorizationGrant().getPrincipal();
      if (var2 instanceof OAuth2ClientAuthenticationToken) {
        var clientPrincipal = (OAuth2ClientAuthenticationToken) var2;
        return clientPrincipal
            .getClientAuthenticationMethod()
            .equals(ClientAuthenticationMethod.NONE);
      }
    }

    return false;
  }

  /**
   * Generates an OAuth2 refresh token with optional region prefix based on the provided context.
   *
   * <p>The region is determined from the authorization code in the context if available, otherwise
   * falls back to the configured default region. This ensures that tokens generated during
   * cross-region authorization code exchange maintain the original region prefix.
   *
   * @param context the OAuth2 token context containing client and authorization information
   * @return a new {@link OAuth2RefreshToken} with appropriate prefix and expiration, or {@code
   *     null} if a refresh token should not be generated for this context
   */
  @Nullable
  @Override
  public OAuth2RefreshToken generate(OAuth2TokenContext context) {
    if (!OAuth2TokenType.REFRESH_TOKEN.equals(context.getTokenType())) {
      return null;
    } else if (isPublicClientForAuthorizationCodeGrant(context)) {
      return null;
    } else {
      var issuedAt = Instant.now();
      var expiresAt =
          issuedAt.plus(
              context.getRegisteredClient().getTokenSettings().getRefreshTokenTimeToLive());
      var tokenValue = this.refreshTokenGenerator.generateKey();
      // TODO: Proper annotations and separate services
      if (Arrays.stream(environment.getActiveProfiles())
          .anyMatch(profile -> profile.equalsIgnoreCase("saas"))) {
        var region = getRegionFromContext(context);
        var prefix = !region.isBlank() ? region + ":" : "";
        return new OAuth2RefreshToken(prefix + tokenValue, issuedAt, expiresAt);
      } else {
        return new OAuth2RefreshToken(tokenValue, issuedAt, expiresAt);
      }
    }
  }
}
