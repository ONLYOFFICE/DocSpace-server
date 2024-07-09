// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.registration.service.mapper;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.entity.Consent;
import com.asc.common.core.domain.value.ConsentId;
import com.asc.common.core.domain.value.enums.ConsentStatus;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import java.time.ZonedDateTime;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ConsentDataMapperTest {
  private ConsentDataMapper consentDataMapper;

  @BeforeEach
  void setUp() {
    consentDataMapper = new ConsentDataMapper();
  }

  @Test
  void testToConsentResponse() {
    var consent = createConsent();
    var clientInfoResponse = createClientInfoResponse();
    var response = consentDataMapper.toConsentResponse(consent, clientInfoResponse);

    assertNotNull(response);
    assertEquals(consent.getId().getRegisteredClientId(), response.getRegisteredClientId());
    assertEquals(consent.getId().getPrincipalName(), response.getPrincipalName());
    assertEquals(String.join(",", consent.getScopes()), response.getScopes());
    assertEquals(consent.getStatus().equals(ConsentStatus.INVALIDATED), response.isInvalidated());
    assertEquals(consent.getModifiedOn(), response.getModifiedOn());
    assertEquals(clientInfoResponse, response.getClient());
  }

  @Test
  void testToConsentResponse_NullConsent() {
    var clientInfoResponse = createClientInfoResponse();
    var exception =
        assertThrows(
            IllegalArgumentException.class,
            () -> consentDataMapper.toConsentResponse(null, clientInfoResponse));

    assertEquals("Consent cannot be null", exception.getMessage());
  }

  @Test
  void testToConsentResponse_NullClientInfoResponse() {
    var consent = createConsent();
    var exception =
        assertThrows(
            IllegalArgumentException.class,
            () -> consentDataMapper.toConsentResponse(consent, null));

    assertEquals("ClientResponse cannot be null", exception.getMessage());
  }

  private Consent createConsent() {
    return Consent.Builder.builder()
        .id(new ConsentId("clientId", "principalName"))
        .scopes(Set.of("read", "write"))
        .status(ConsentStatus.ACTIVE)
        .modifiedOn(ZonedDateTime.now())
        .build();
  }

  private ClientInfoResponse createClientInfoResponse() {
    return ClientInfoResponse.builder()
        .name("Test Client")
        .clientId("clientId")
        .description("Test Description")
        .websiteUrl("http://test.com")
        .termsUrl("http://test.com/terms")
        .policyUrl("http://test.com/policy")
        .logo("Test Logo")
        .authenticationMethods(Set.of("client_secret_post", "pkce"))
        .scopes(Set.of("read", "write"))
        .createdOn(ZonedDateTime.now())
        .createdBy("creator")
        .modifiedOn(ZonedDateTime.now())
        .modifiedBy("modifier")
        .build();
  }
}
