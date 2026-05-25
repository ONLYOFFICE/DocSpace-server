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
