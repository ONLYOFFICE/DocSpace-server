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

package com.asc.common.utilities;

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
