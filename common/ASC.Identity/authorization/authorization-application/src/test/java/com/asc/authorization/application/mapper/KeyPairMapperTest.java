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

package com.asc.authorization.application.mapper;

import static org.junit.jupiter.api.Assertions.*;

import java.security.KeyPair;
import java.security.KeyPairGenerator;
import java.security.NoSuchAlgorithmException;
import java.security.spec.InvalidKeySpecException;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

public class KeyPairMapperTest {
  private KeyPairMapper keyPairMapper;
  private KeyPair rsaKeyPair;

  @BeforeEach
  void setUp() throws NoSuchAlgorithmException {
    keyPairMapper = new KeyPairMapper();

    var keyPairGenerator = KeyPairGenerator.getInstance("RSA");
    keyPairGenerator.initialize(2048);

    rsaKeyPair = keyPairGenerator.generateKeyPair();
  }

  @Test
  void whenPublicKeyIsConvertedToString_thenBase64StringIsReturned() {
    var publicKey = rsaKeyPair.getPublic();
    var keyString = keyPairMapper.toString(publicKey);

    assertFalse(keyString.isEmpty());
    assertNotNull(keyString);
  }

  @Test
  void whenPrivateKeyIsConvertedToString_thenBase64StringIsReturned() {
    var privateKey = rsaKeyPair.getPrivate();
    var keyString = keyPairMapper.toString(privateKey);

    assertFalse(keyString.isEmpty());
    assertNotNull(keyString);
  }

  @Test
  void whenBase64StringIsConvertedToPublicKey_thenPublicKeyIsReturned()
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    var originalPublicKey = rsaKeyPair.getPublic();
    var keyString = keyPairMapper.toString(originalPublicKey);
    var restoredPublicKey = keyPairMapper.toPublicKey(keyString, "RSA");

    assertNotNull(restoredPublicKey);
    assertEquals(originalPublicKey.getAlgorithm(), restoredPublicKey.getAlgorithm());
    assertArrayEquals(originalPublicKey.getEncoded(), restoredPublicKey.getEncoded());
  }

  @Test
  void whenBase64StringIsConvertedToPrivateKey_thenPrivateKeyIsReturned()
      throws NoSuchAlgorithmException, InvalidKeySpecException {
    var originalPrivateKey = rsaKeyPair.getPrivate();
    var keyString = keyPairMapper.toString(originalPrivateKey);
    var restoredPrivateKey = keyPairMapper.toPrivateKey(keyString, "RSA");

    assertNotNull(restoredPrivateKey);
    assertEquals(originalPrivateKey.getAlgorithm(), restoredPrivateKey.getAlgorithm());
    assertArrayEquals(originalPrivateKey.getEncoded(), restoredPrivateKey.getEncoded());
  }

  @Test
  void whenInvalidAlgorithmIsUsedForPublicKey_thenExceptionIsThrown() {
    var publicKey = rsaKeyPair.getPublic();
    var keyString = keyPairMapper.toString(publicKey);

    assertThrows(
        NoSuchAlgorithmException.class,
        () -> keyPairMapper.toPublicKey(keyString, "INVALID_ALGORITHM"));
  }

  @Test
  void whenInvalidAlgorithmIsUsedForPrivateKey_thenExceptionIsThrown() {
    var privateKey = rsaKeyPair.getPrivate();
    var keyString = keyPairMapper.toString(privateKey);

    assertThrows(
        NoSuchAlgorithmException.class,
        () -> keyPairMapper.toPrivateKey(keyString, "INVALID_ALGORITHM"));
  }

  @Test
  void whenInvalidBase64StringIsUsedForPublicKey_thenExceptionIsThrown() {
    assertThrows(
        IllegalArgumentException.class, () -> keyPairMapper.toPublicKey("non-base64", "RSA"));
  }

  @Test
  void whenInvalidBase64StringIsUsedForPrivateKey_thenExceptionIsThrown() {
    assertThrows(
        IllegalArgumentException.class, () -> keyPairMapper.toPrivateKey("non-base64", "RSA"));
  }
}
