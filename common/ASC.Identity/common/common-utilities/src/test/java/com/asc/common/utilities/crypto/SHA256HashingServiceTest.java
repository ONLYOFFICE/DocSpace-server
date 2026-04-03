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

package com.asc.common.utilities.crypto;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import org.junit.jupiter.api.Test;

class SHA256HashingServiceTest {
  private final SHA256HashingService service = new SHA256HashingService();

  private static String sha256Hex(String data) throws Exception {
    var digest = MessageDigest.getInstance("SHA-256");
    var hash = digest.digest(data.getBytes(StandardCharsets.UTF_8));

    var hex = new StringBuilder();
    for (byte b : hash) {
      var v = b & 0xFF;
      if (v < 0x10) hex.append('0');
      hex.append(Integer.toHexString(v));
    }
    return hex.toString();
  }

  @Test
  void givenInput_whenHashing_thenMatchesJavaSha256Hex() throws Exception {
    var data = "hello";
    var expected = sha256Hex(data);

    assertEquals(expected, service.hash(data));
  }

  @Test
  void givenDataMatchesHash_whenVerifying_thenReturnsTrue() throws Exception {
    var data = "hello";
    var hashed = service.hash(data);

    assertTrue(service.verify(data, hashed));
  }

  @Test
  void givenDataDoesNotMatchHash_whenVerifying_thenReturnsFalse() throws Exception {
    var hashed = service.hash("hello");

    assertFalse(service.verify("goodbye", hashed));
  }

  @Test
  void givenNullInput_whenHashing_thenReturnsNull() {
    assertNull(service.hash(null));
  }
}
