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

import java.time.Instant;
import java.util.Arrays;
import java.util.Base64;
import org.springframework.core.env.Environment;
import org.springframework.lang.Nullable;
import org.springframework.security.crypto.keygen.Base64StringKeyGenerator;
import org.springframework.security.crypto.keygen.StringKeyGenerator;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenGenerator;

/**
 * Custom OAuth2 authorization code generator that adds a region-based prefix to generated
 * authorization codes.
 *
 * <p>This generator extends the default Spring Authorization Server functionality by prepending a
 * region-based prefix to authorization codes before they are stored. The prefix helps identify
 * authorization codes by region and can be used for debugging, routing, or multi-region support.
 *
 * <p>The generator uses a cryptographically secure Base64 string generator to create the
 * authorization code value, then adds the region prefix followed by an underscore.
 *
 * @see OAuth2TokenGenerator
 * @see OAuth2AuthorizationCode
 */
public class PrefixedAuthorizationCodeGenerator
    implements OAuth2TokenGenerator<OAuth2AuthorizationCode> {
  private Environment environment;

  private final StringKeyGenerator authorizationCodeGenerator =
      new Base64StringKeyGenerator(Base64.getUrlEncoder().withoutPadding(), 96);
  private final String prefix;

  /**
   * Constructs a {@code PrefixedAuthorizationCodeGenerator} with a region-based prefix.
   *
   * @param region the region to use as a prefix (e.g., "eu", "us", "local"). The region will be
   *     converted to uppercase and an underscore will be appended.
   */
  public PrefixedAuthorizationCodeGenerator(Environment environment, String region) {
    this.environment = environment;
    this.prefix = region != null && !region.isBlank() ? region.toLowerCase() + ":" : "";
  }

  /**
   * Generates an OAuth2 authorization code with a prefix.
   *
   * <p>This method creates a new authorization code by generating a secure random string and
   * prepending the configured prefix. The generated code includes both issued and expiry timestamps
   * based on the client's configuration.
   *
   * @param context the {@link OAuth2TokenContext} containing information about the token request.
   * @return a new {@link OAuth2AuthorizationCode} with the prefix, or {@code null} if the context
   *     is not for an authorization code.
   */
  @Nullable
  @Override
  public OAuth2AuthorizationCode generate(OAuth2TokenContext context) {
    if (context.getTokenType() == null
        || !OAuth2ParameterNames.CODE.equals(context.getTokenType().getValue())) return null;

    var issuedAt = Instant.now();
    var expiresAt =
        issuedAt.plus(
            context.getRegisteredClient().getTokenSettings().getAuthorizationCodeTimeToLive());

    var codeValue = this.authorizationCodeGenerator.generateKey();
    if (Arrays.stream(environment.getActiveProfiles())
        .anyMatch(profile -> profile.equalsIgnoreCase("saas")))
      return new OAuth2AuthorizationCode(this.prefix + codeValue, issuedAt, expiresAt);
    else return new OAuth2AuthorizationCode(codeValue, issuedAt, expiresAt);
  }
}
