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
 * validation. HTTP, HTTPS, and custom protocols like claude: are allowed.
 */
@Slf4j
@Component
public class URLCollectionValidator
    implements ConstraintValidator<URLCollection, Collection<String>> {
  private static final Set<String> ALLOWED_PROTOCOLS = Set.of("http", "https", "claude");

  /**
   * Validates the collection of URLs using URI/URL validation. HTTP, HTTPS, and custom protocols
   * like claude: are allowed.
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

        if ("http".equalsIgnoreCase(protocol) || "https".equalsIgnoreCase(protocol)) {
          var validatedUrl = uri.toURL();
          log.debug("URL is valid: {}", validatedUrl);
        } else {
          log.debug("Custom protocol URI is valid: {}", uri);
        }
      } catch (Exception e) {
        log.warn("Invalid URL detected: {} - {}", url, e.getMessage());
        return false;
      }
    }

    return true;
  }
}
