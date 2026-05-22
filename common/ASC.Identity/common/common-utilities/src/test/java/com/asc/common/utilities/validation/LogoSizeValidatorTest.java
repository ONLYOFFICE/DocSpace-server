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

import jakarta.validation.Payload;
import java.lang.annotation.Annotation;
import java.util.Base64;
import org.junit.jupiter.api.Test;

class LogoSizeValidatorTest {

  private static LogoSize logoSize(int maxLength, long maxBytes) {
    return new LogoSize() {
      public String message() {
        return "test";
      }

      public long maxBytes() {
        return maxBytes;
      }

      public int maxLength() {
        return maxLength;
      }

      public Class<?>[] groups() {
        return new Class<?>[0];
      }

      @SuppressWarnings("unchecked")
      public Class<? extends Payload>[] payload() {
        return new Class[0];
      }

      public Class<? extends Annotation> annotationType() {
        return LogoSize.class;
      }
    };
  }

  @Test
  void givenNullOrEmptyValue_whenValidating_thenReturnsTrue() {
    var validator = new LogoSizeValidator();
    var annotation = logoSize(10, 10L);
    validator.initialize(annotation);

    assertTrue(validator.isValid(null, null));
    assertTrue(validator.isValid("", null));
  }

  @Test
  void givenValueLengthExceedsMaxLength_whenValidating_thenReturnsFalse() {
    var validator = new LogoSizeValidator();
    var annotation = logoSize(5, 10L);
    validator.initialize(annotation);

    assertFalse(validator.isValid("AAAAAA", null));
  }

  @Test
  void givenDecodedByteArrayExceedsMaxBytes_whenValidating_thenReturnsFalse() {
    var validator = new LogoSizeValidator();
    var annotation = logoSize(100, 4L);
    validator.initialize(annotation);

    var base64 = Base64.getEncoder().encodeToString("hello".getBytes());
    assertFalse(validator.isValid(base64, null));
  }

  @Test
  void givenValueWithinLimitsWithDataPrefix_whenValidating_thenReturnsTrue() {
    var validator = new LogoSizeValidator();
    var annotation = logoSize(100, 5L);
    validator.initialize(annotation);

    var base64 = Base64.getEncoder().encodeToString("hello".getBytes());
    var dataPrefixValue = "data:image/png;base64," + base64;

    assertTrue(validator.isValid(dataPrefixValue, null));
  }
}
