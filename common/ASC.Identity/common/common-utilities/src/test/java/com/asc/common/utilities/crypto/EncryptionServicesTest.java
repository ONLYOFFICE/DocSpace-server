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

package com.asc.common.utilities.crypto;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class EncryptionServicesTest {
  @Test
  void givenPlaintext_whenNoOpEncryptAndDecrypt_thenIdentity() {
    var service = new NoOpEncryptionService();

    assertEquals("plain", service.encrypt("plain"));
    assertEquals("plain", service.decrypt("plain"));

    assertEquals("", service.encrypt(""));
    assertEquals("", service.decrypt(""));
  }

  @Test
  void givenPlaintext_whenEncryptThenDecrypt_thenReturnsOriginalPlainText() {
    var service = new AesEncryptionService("my-secret");
    var plainText = "secret-message";

    var encrypted = service.encrypt(plainText);

    assertTrue(encrypted != null && !encrypted.isBlank(), "Encrypted output should be non-empty");
    assertNotEquals(plainText, encrypted);

    var decrypted = service.decrypt(encrypted);
    assertEquals(plainText, decrypted);
  }

  @Test
  void givenInvalidCipherText_whenDecrypting_thenThrowsDecryptionException() {
    var service = new AesEncryptionService("my-secret");

    assertThrows(DecryptionException.class, () -> service.decrypt("not-base64"));
  }
}
