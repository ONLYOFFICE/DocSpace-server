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

package com.asc.registration.application.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

import com.asc.common.application.proto.Consent;
import com.asc.common.application.proto.GetConsentsResponse;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.cleanup.RemovedClientReporter;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableModificationResponse;
import java.time.ZonedDateTime;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ConsentServiceTest {
  private static final String PRINCIPAL = "principal";
  private static final String LIVE = "11111111-1111-1111-1111-111111111111";
  private static final String REMOVED = "22222222-2222-2222-2222-222222222222";
  private static final String MODIFIED = "2026-10-29T10:00:00Z";
  private static final String CURSOR = "2026-10-29T09:00:00Z";

  private GrpcAuthorizationService grpcAuthorizationService;
  private ClientApplicationService clientApplicationService;
  private RemovedClientReporter removedClientReporter;
  private ConsentService consentService;

  @BeforeEach
  void setUp() {
    grpcAuthorizationService = mock(GrpcAuthorizationService.class);
    clientApplicationService = mock(ClientApplicationService.class);
    removedClientReporter = mock(RemovedClientReporter.class);
    consentService =
        new ConsentService(
            grpcAuthorizationService, clientApplicationService, removedClientReporter);
  }

  private static List<ConsentResponse> data(PageableModificationResponse<ConsentResponse> page) {
    var data = new ArrayList<ConsentResponse>();
    page.getData().forEach(data::add);
    return data;
  }

  private static Consent consent(String clientId) {
    return Consent.newBuilder()
        .setClientId(clientId)
        .addScopes("openid")
        .setModifiedAt(MODIFIED)
        .build();
  }

  private void authorizationReturns(String cursor, Consent... consents) {
    when(grpcAuthorizationService.getConsents(eq(PRINCIPAL), anyInt(), any()))
        .thenReturn(
            GetConsentsResponse.newBuilder()
                .addAllConsents(List.of(consents))
                .setLastModifiedAt(cursor)
                .build());
  }

  private void storeHolds(String... clientIds) {
    when(clientApplicationService.getClients(anyList()))
        .thenReturn(
            java.util.Arrays.stream(clientIds)
                .map(id -> ClientResponse.builder().clientId(id).name("app").build())
                .toList());
  }

  @Test
  void givenConsentOfRemovedClient_whenListing_thenDroppedAndClientReported() {
    authorizationReturns(CURSOR, consent(LIVE), consent(REMOVED));
    storeHolds(LIVE);

    var page = consentService.getConsents(PRINCIPAL, 10, null);

    assertEquals(
        List.of(LIVE), data(page).stream().map(ConsentResponse::getRegisteredClientId).toList());
    verify(removedClientReporter).report(REMOVED, ZonedDateTime.parse(MODIFIED).toInstant());
    verify(removedClientReporter, never()).report(eq(LIVE), any());
  }

  @Test
  void givenOnlyRemovedClients_whenListing_thenPageEmptyButKeepsCursor() {
    authorizationReturns(CURSOR, consent(REMOVED));
    storeHolds();

    var page = consentService.getConsents(PRINCIPAL, 10, null);

    assertEquals(List.of(), data(page));
    assertEquals(ZonedDateTime.parse(CURSOR), page.getLastModifiedOn());
  }

  @Test
  void givenLiveClients_whenListing_thenEveryConsentKeptWithDetailsAndNothingReported() {
    authorizationReturns("", consent(LIVE));
    storeHolds(LIVE);

    var page = consentService.getConsents(PRINCIPAL, 10, null);

    assertEquals(1, data(page).size());
    assertEquals("app", data(page).getFirst().getClient().getName());
    verify(removedClientReporter, never()).report(anyString(), any());
  }

  @Test
  void whenListing_thenClientsLookedUpSingleBatch() {
    authorizationReturns("", consent(LIVE), consent(REMOVED));
    storeHolds(LIVE);

    consentService.getConsents(PRINCIPAL, 10, null);

    verify(clientApplicationService, times(1)).getClients(anyList());
    verify(clientApplicationService, never()).getClient(anyString());
  }
}
