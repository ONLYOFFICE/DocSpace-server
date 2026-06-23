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

import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;

/**
 * The NoOpEncryptionService class provides a no-operation implementation of the EncryptionService
 * interface. This implementation simply returns the input text or cipher without performing any
 * encryption or decryption.
 */
@Component
@ConditionalOnProperty(value = "spring.application.encryption.type", havingValue = "noop")
public class NoOpEncryptionService implements EncryptionService {

  /**
   * Encrypts the given text. This implementation simply returns the input text as is.
   *
   * @param text the text to be encrypted
   * @return the input text as is (no encryption is performed)
   * @throws EncryptionException if an error occurs during encryption (not thrown in this
   *     implementation)
   */
  public String encrypt(String text) throws EncryptionException {
    return text;
  }

  /**
   * Decrypts the given cipher text. This implementation simply returns the input cipher as is.
   *
   * @param cipher the cipher text to be decrypted
   * @return the input cipher as is (no decryption is performed)
   * @throws DecryptionException if an error occurs during decryption (not thrown in this
   *     implementation)
   */
  public String decrypt(String cipher) throws DecryptionException {
    return cipher;
  }
}
