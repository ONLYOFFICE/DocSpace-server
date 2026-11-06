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

package com.asc.authorization.application.security.oauth.service;

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.asc.authorization.application.exception.client.NonRetryableGrpcException;
import com.asc.common.application.proto.ClientRegistrationServiceGrpc;
import com.asc.common.application.proto.GetClientRequest;
import io.grpc.Deadline;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.EnumSource;

class GrpcRegisteredClientServiceTest {
  private GrpcRegisteredClientService serviceAnswering(Status status) {
    var stub = mock(ClientRegistrationServiceGrpc.ClientRegistrationServiceBlockingStub.class);
    when(stub.withDeadline(any(Deadline.class))).thenReturn(stub);
    when(stub.getClient(any(GetClientRequest.class))).thenThrow(status.asRuntimeException());

    var service = new GrpcRegisteredClientService(800);
    service.registrationService = stub;
    return service;
  }

  @ParameterizedTest
  @EnumSource(
      value = Status.Code.class,
      names = {"NOT_FOUND", "INVALID_ARGUMENT", "INTERNAL"})
  void givenFailureOfOneClient_whenGettingClient_thenNotRetryable(Status.Code code) {
    var service = serviceAnswering(Status.fromCode(code));

    assertThrows(NonRetryableGrpcException.class, () -> service.getClient("client"));
  }

  @ParameterizedTest
  @EnumSource(
      value = Status.Code.class,
      names = {"UNAVAILABLE", "RESOURCE_EXHAUSTED"})
  void givenFailureOfTheLookup_whenGettingClient_thenRetryable(Status.Code code) {
    var service = serviceAnswering(Status.fromCode(code));

    assertThrows(StatusRuntimeException.class, () -> service.getClient("client"));
  }
}
