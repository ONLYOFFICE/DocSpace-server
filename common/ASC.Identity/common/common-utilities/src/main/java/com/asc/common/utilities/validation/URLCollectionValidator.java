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

package com.asc.common.utilities.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import java.net.URI;
import java.util.Collection;
import java.util.Set;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

/**
 * The URLCollectionValidator class is responsible for validating a collection of URLs using URI/URL
 * validation. Only HTTP and HTTPS protocols are allowed.
 */
@Slf4j
@Component
public class URLCollectionValidator
    implements ConstraintValidator<URLCollection, Collection<String>> {
  private static final Set<String> ALLOWED_PROTOCOLS = Set.of("http", "https");

  /**
   * Validates the collection of URLs using URI/URL validation. Only HTTP and HTTPS protocols are
   * allowed.
   *
   * @param urls the collection of URLs to be validated
   * @param context the validation context
   * @return true if all URLs are valid, false otherwise
   */
  public boolean isValid(Collection<String> urls, ConstraintValidatorContext context) {
    if (urls == null) return false;

    for (var url : urls) {
      log.debug("Validating URL: {}", url);
      if (url == null || url.isEmpty()) {
        log.warn("Invalid URL detected: {}", url);
        return false;
      }

      try {
        var uri = URI.create(url);
        var protocol = uri.getScheme();

        if (protocol == null || !ALLOWED_PROTOCOLS.contains(protocol.toLowerCase())) {
          log.warn("Invalid protocol detected: {} for URL: {}", protocol, url);
          return false;
        }

        var validatedUrl = uri.toURL();
        log.debug("URL is valid: {}", validatedUrl);
      } catch (Exception e) {
        log.warn("Invalid URL detected: {} - {}", url, e.getMessage());
        return false;
      }
    }
    return true;
  }
}
