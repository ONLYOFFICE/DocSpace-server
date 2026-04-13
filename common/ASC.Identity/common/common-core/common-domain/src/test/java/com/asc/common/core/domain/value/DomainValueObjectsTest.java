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
