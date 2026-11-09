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
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.application.proto.ClientResponse;
import com.asc.common.application.proto.GetClientRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import io.grpc.stub.StreamObserver;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

class AbstractGrpcClientRegistrationServiceTest {
  private static final String CLIENT = "client";

  private ClientApplicationService clientApplicationService;
  private AbstractGrpcClientRegistrationService service;
  private StreamObserver<ClientResponse> observer;

  @BeforeEach
  @SuppressWarnings("unchecked")
  void setUp() {
    clientApplicationService = mock(ClientApplicationService.class);
    service = new AbstractGrpcClientRegistrationService(clientApplicationService) {};
    observer = mock(StreamObserver.class);
  }

  private void getClient() {
    service.getClient(GetClientRequest.newBuilder().setClientId(CLIENT).build(), observer);
  }

  @Test
  void givenClientWithoutOptionalFields_whenGettingClient_thenAnswersWithThemEmpty() {
    when(clientApplicationService.getStoredClient(CLIENT))
        .thenReturn(
            com.asc.common.service.transfer.response.ClientResponse.builder()
                .clientId(CLIENT)
                .tenant(1)
                .scopes(Set.of("files:read"))
                .enabled(true)
                .build());

    getClient();

    var answer = ArgumentCaptor.forClass(ClientResponse.class);

    verify(observer).onNext(answer.capture());
    verify(observer).onCompleted();
    assertEquals(CLIENT, answer.getValue().getClientId());
    assertEquals("", answer.getValue().getDescription());
    assertEquals("", answer.getValue().getClientSecret());
    assertEquals("", answer.getValue().getWebsiteUrl());
    assertEquals(0, answer.getValue().getRedirectUrisCount());
    assertEquals(Set.of("files:read"), Set.copyOf(answer.getValue().getScopesList()));
    assertFalse(answer.getValue().hasCreatedOn());
  }

  @Test
  void givenClientTimes_whenGettingClient_thenAnswersWithEpochSeconds() {
    var createdOn = ZonedDateTime.of(2026, 11, 6, 10, 15, 30, 500, ZoneOffset.UTC);
    var modifiedOn = createdOn.plusDays(1);
    when(clientApplicationService.getStoredClient(CLIENT))
        .thenReturn(
            com.asc.common.service.transfer.response.ClientResponse.builder()
                .clientId(CLIENT)
                .createdOn(createdOn)
                .modifiedOn(modifiedOn)
                .build());

    getClient();

    var answer = ArgumentCaptor.forClass(ClientResponse.class);

    verify(observer).onNext(answer.capture());
    assertEquals(createdOn.toEpochSecond(), answer.getValue().getCreatedOn().getSeconds());
    assertEquals(500, answer.getValue().getCreatedOn().getNanos());
    assertEquals(modifiedOn.toEpochSecond(), answer.getValue().getModifiedOn().getSeconds());
  }

  @Test
  void givenClientThatCannotBeAnswered_whenGettingClient_thenAnswersInternal() {
    var broken = mock(com.asc.common.service.transfer.response.ClientResponse.class);
    when(broken.getClientId()).thenThrow(new IllegalStateException("corrupt client"));
    when(clientApplicationService.getStoredClient(CLIENT)).thenReturn(broken);

    getClient();

    var error = ArgumentCaptor.forClass(Throwable.class);

    verify(observer).onError(error.capture());
    verify(observer, never()).onNext(org.mockito.ArgumentMatchers.any());
    assertEquals(
        Status.Code.INTERNAL, ((StatusRuntimeException) error.getValue()).getStatus().getCode());
  }

  @Test
  void givenStoreFailure_whenGettingClient_thenAnswersUnavailable() {
    when(clientApplicationService.getStoredClient(CLIENT))
        .thenThrow(new IllegalStateException("store timed out"));

    getClient();

    var error = ArgumentCaptor.forClass(Throwable.class);

    verify(observer).onError(error.capture());
    assertEquals(
        Status.Code.UNAVAILABLE, ((StatusRuntimeException) error.getValue()).getStatus().getCode());
  }
}
