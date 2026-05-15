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

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.Collections;
import java.util.List;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class URLCollectionValidatorTest {
  private URLCollectionValidator validator;

  @BeforeEach
  void setUp() {
    validator = new URLCollectionValidator();
  }

  @Test
  void givenUrlsIsNull_whenValidating_thenReturnsFalse() {
    assertFalse(validator.isValid(null, null));
  }

  @Test
  void givenUrlsContainsNullOrEmptyEntry_whenValidating_thenReturnsFalse() {
    assertFalse(validator.isValid(Collections.singletonList(null), null));
    assertFalse(validator.isValid(List.of(""), null));
  }

  @Test
  void givenUrlsWithDisallowedProtocol_whenValidating_thenReturnsFalse() {
    assertFalse(validator.isValid(Set.of("ftp://example.com"), null));
  }

  @Test
  void givenMalformedUrl_whenValidating_thenReturnsFalse() {
    assertFalse(validator.isValid(Set.of("https://example.com:bad"), null));
  }

  @Test
  void givenValidHttpsUrls_whenValidating_thenReturnsTrue() {
    assertTrue(validator.isValid(Set.of("https://example.com"), null));
  }

  @Test
  void givenValidCustomProtocolUrls_whenValidating_thenReturnsTrue() {
    assertTrue(validator.isValid(Set.of("claude://example.com"), null));
  }
}
