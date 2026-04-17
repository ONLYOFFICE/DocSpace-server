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

import com.asc.authorization.data.consent.entity.ConsentEntity;
import java.time.ZonedDateTime;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;

public class ConsentMapperTest {
  private ConsentMapper consentMapper;

  @BeforeEach
  void setUp() {
    consentMapper = new ConsentMapper();
  }

  @Test
  void whenConsentIsMappedToEntity_thenConsentEntityIsCreated() {
    var registeredClientId = "client";
    var principalName = "user";
    var authorizationConsent =
        OAuth2AuthorizationConsent.withId(registeredClientId, principalName)
            .scope("read")
            .scope("write")
            .scope("openid")
            .build();

    var entity = consentMapper.toEntity(authorizationConsent);

    assertEquals(registeredClientId, entity.getRegisteredClientId());
    assertEquals(principalName, entity.getPrincipalId());
    assertEquals(Set.of("read", "write", "openid"), entity.getScopes());
    assertNotNull(entity.getModifiedAt());
    assertFalse(entity.isInvalidated());
  }

  @Test
  void whenConsentEntityIsMappedToConsent_thenConsentIsCreated() {
    var entity =
        ConsentEntity.builder()
            .registeredClientId("client")
            .principalId("user")
            .scopes(Set.of("profile", "email"))
            .modifiedAt(ZonedDateTime.now())
            .invalidated(false)
            .build();

    var consent = consentMapper.toConsent(entity);

    assertEquals("client", consent.getRegisteredClientId());
    assertEquals("user", consent.getPrincipalName());
    assertTrue(consent.getScopes().contains("profile"));
    assertTrue(consent.getScopes().contains("email"));
  }

  @Test
  void whenConsentWithSingleScopeIsMapped_thenEntityHasSingleScope() {
    var authorizationConsent =
        OAuth2AuthorizationConsent.withId("client", "user").scope("read").build();

    var entity = consentMapper.toEntity(authorizationConsent);

    assertEquals(1, entity.getScopes().size());
    assertTrue(entity.getScopes().contains("read"));
  }

  @Test
  void whenConsentRoundTripIsPerformed_thenDataIsPreserved() {
    var originalConsent =
        OAuth2AuthorizationConsent.withId("client", "user").scope("read").scope("write").build();

    var entity = consentMapper.toEntity(originalConsent);
    var restoredConsent = consentMapper.toConsent(entity);

    assertEquals(originalConsent.getRegisteredClientId(), restoredConsent.getRegisteredClientId());
    assertEquals(originalConsent.getPrincipalName(), restoredConsent.getPrincipalName());
    assertEquals(originalConsent.getScopes(), restoredConsent.getScopes());
  }

  @Test
  void whenConsentEntityIsMapped_thenModifiedAtIsSet() {
    var beforeMapping = ZonedDateTime.now().minusSeconds(1);
    var authorizationConsent =
        OAuth2AuthorizationConsent.withId("client", "user").scope("read").build();

    var entity = consentMapper.toEntity(authorizationConsent);

    assertNotNull(entity.getModifiedAt());
    assertTrue(entity.getModifiedAt().isAfter(beforeMapping));
  }

  @Test
  void whenConsentEntityIsMapped_thenInvalidatedIsFalse() {
    var authorizationConsent =
        OAuth2AuthorizationConsent.withId("client", "user").scope("read").build();

    var entity = consentMapper.toEntity(authorizationConsent);

    assertFalse(entity.isInvalidated());
  }
}
