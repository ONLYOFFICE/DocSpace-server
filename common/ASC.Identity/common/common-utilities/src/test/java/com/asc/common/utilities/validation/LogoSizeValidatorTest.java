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
