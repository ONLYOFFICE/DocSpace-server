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

package com.asc.registration.container.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.Mockito.*;

import com.asc.common.application.proto.ClientResponse;
import com.asc.common.application.proto.GetClientRequest;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import io.grpc.stub.StreamObserver;
import jakarta.validation.ConstraintViolationException;
import java.util.Set;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

class GrpcClientRegistrationServiceTest {
  private static final String CLIENT = "client";

  private ClientApplicationService clientApplicationService;
  private GrpcClientRegistrationService service;
  private StreamObserver<ClientResponse> observer;

  @BeforeEach
  @SuppressWarnings("unchecked")
  void setUp() {
    clientApplicationService = mock(ClientApplicationService.class);
    service = new GrpcClientRegistrationService(clientApplicationService);
    observer = mock(StreamObserver.class);
  }

  private Status.Code codeFor(RuntimeException failure) {
    when(clientApplicationService.getStoredClient(CLIENT)).thenThrow(failure);

    service.getClient(GetClientRequest.newBuilder().setClientId(CLIENT).build(), observer);

    var error = ArgumentCaptor.forClass(Throwable.class);

    verify(observer).onError(error.capture());

    return ((StatusRuntimeException) error.getValue()).getStatus().getCode();
  }

  @Test
  void givenMissingClient_whenGettingClient_thenAnswersNotFound() {
    assertEquals(Status.Code.NOT_FOUND, codeFor(new ClientNotFoundException("gone")));
  }

  @Test
  void givenInvalidRequest_whenGettingClient_thenAnswersInvalidArgument() {
    assertEquals(
        Status.Code.INVALID_ARGUMENT, codeFor(new ConstraintViolationException("blank", Set.of())));
  }

  @Test
  void givenStoreFailure_whenGettingClient_thenAnswersUnavailableRatherThanNotFound() {
    assertEquals(Status.Code.UNAVAILABLE, codeFor(new IllegalStateException("store timed out")));
  }
}
