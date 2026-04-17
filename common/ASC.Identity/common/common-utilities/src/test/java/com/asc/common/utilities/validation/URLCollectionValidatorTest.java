// (c) Copyright Ascensio System SIA 2009-2026
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
