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

package com.asc.common.core.domain.value;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotEquals;

import java.util.UUID;
import org.junit.jupiter.api.Test;

class DomainValueObjectsTest {
  @Test
  void whenClientIdValuesAreEqual_thenIdsAreEqual() {
    var uuid = UUID.randomUUID();
    var idOne = new ClientId(uuid);
    var idTwo = new ClientId(uuid);

    assertEquals(uuid, idOne.getValue());
    assertEquals(idOne, idTwo);
    assertEquals(idOne.hashCode(), idTwo.hashCode());
  }

  @Test
  void whenClientIdValuesAreNull_thenIdsAreEqual() {
    var idOne = new ClientId(null);
    var idTwo = new ClientId(null);

    assertEquals(idOne, idTwo);
    assertEquals(idOne.hashCode(), idTwo.hashCode());
  }

  @Test
  void whenBaseIdTypesDiffer_thenIdsAreNotEqual() {
    var clientId = new ClientId(UUID.randomUUID());
    var userId = new UserId("user-1");

    assertNotEquals(clientId, userId);
  }

  @Test
  void whenClientSecretValuesAreEqual_thenSecretsAreEqual() {
    var secretOne = new ClientSecret("secret-1");
    var secretTwo = new ClientSecret("secret-1");

    assertEquals("secret-1", secretOne.value());
    assertEquals(secretOne, secretTwo);
    assertEquals(secretOne.hashCode(), secretTwo.hashCode());
  }

  @Test
  void whenConsentIdValuesAreEqual_thenConsentIdsAreEqual() {
    var consentOne = new ConsentId("client-1", "principal-1");
    var consentTwo = new ConsentId("client-1", "principal-1");

    assertEquals("client-1", consentOne.getRegisteredClientId());
    assertEquals("principal-1", consentOne.getPrincipalName());
    assertEquals(consentOne, consentTwo);
    assertEquals(consentOne.hashCode(), consentTwo.hashCode());
  }

  @Test
  void whenConsentIdValuesDiffer_thenConsentIdsAreNotEqual() {
    var consentOne = new ConsentId("client-1", "principal-1");
    var consentTwo = new ConsentId("client-2", "principal-1");

    assertNotEquals(consentOne, consentTwo);
  }
}
