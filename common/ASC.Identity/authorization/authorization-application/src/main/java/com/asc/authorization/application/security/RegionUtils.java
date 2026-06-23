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

package com.asc.authorization.application.security;

import com.nimbusds.jwt.SignedJWT;
import java.util.Optional;
import java.util.function.Function;
import java.util.regex.Pattern;

/**
 * Utility class for extracting region information from tokens.
 *
 * <p>Provides methods to extract region identifiers from token prefixes in the format "[region]:"
 * or "region:", with support for custom extraction strategies.
 */
public final class RegionUtils {
  public static final Function<String, Optional<String>> JWT_REGION_EXTRACTOR =
      token -> {
        try {
          var jwt = SignedJWT.parse(token);
          var claims = jwt.getJWTClaimsSet();
          var regionClaim = claims.getStringClaim("region");
          return Optional.ofNullable(regionClaim).map(String::toLowerCase);
        } catch (Exception e) {
          return Optional.empty();
        }
      };
  private static final Pattern REGION_PATTERN = Pattern.compile("\\[?([a-zA-Z0-9_\\s-]+)\\]?:");

  private RegionUtils() {}

  /**
   * Extracts the region from a token prefix.
   *
   * <p>The region prefix is expected at the beginning of the token in the format "[region]:" or
   * "region:". Brackets are preserved if present.
   *
   * @param token the token string with potential region prefix
   * @return an Optional containing the region (with brackets if present) if found, empty otherwise
   */
  public static Optional<String> extractFromPrefix(String token) {
    if (token == null) return Optional.empty();
    var matcher = REGION_PATTERN.matcher(token);
    if (matcher.find()) return Optional.of(matcher.group(0).replaceAll(":$", ""));
    return Optional.empty();
  }

  /**
   * Extracts the region from a token using a custom extractor with prefix fallback.
   *
   * <p>Tries the custom extractor first, then falls back to prefix extraction if the custom
   * extractor returns empty.
   *
   * @param token the raw token
   * @param customExtractor a function that attempts to extract the region from the token (e.g., JWT
   *     claim extraction)
   * @return an Optional containing the region if found, empty otherwise
   */
  public static Optional<String> extract(
      String token, Function<String, Optional<String>> customExtractor) {
    if (token == null) return Optional.empty();
    var region = customExtractor.apply(token);
    if (region.isPresent()) return region;
    return extractFromPrefix(token);
  }
}
