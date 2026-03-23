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
