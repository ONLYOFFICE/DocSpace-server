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

import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.common.application.proto.ClientResponse;
import io.github.resilience4j.retry.annotation.Retry;
import io.grpc.Deadline;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import net.devh.boot.grpc.client.inject.GrpcClient;
import org.springframework.stereotype.Service;

/**
 * Service for interacting with the gRPC client registration service.
 *
 * <p>This service provides methods to retrieve client information from a gRPC service. It includes
 * retry logic for handling transient errors.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class GrpcRegisteredClientService {

  /** The gRPC client stub for the registration service. */
  @GrpcClient("registrationService")
  com.asc.common.application.proto.ClientRegistrationServiceGrpc
          .ClientRegistrationServiceBlockingStub
      registrationService;

  /**
   * Retrieves a client by its ID from the gRPC service.
   *
   * <p>This method includes retry logic to handle transient errors. It will not retry if a {@link
   * RegisteredClientPermissionException} is thrown.
   *
   * @param id the ID of the client to retrieve.
   * @return the {@link ClientResponse} containing the client information.
   * @throws RegisteredClientPermissionException if the client is not accessible.
   */
  @Retry(name = "grpcClientRetry")
  public ClientResponse getClient(String id) {
    log.info("GRPC call to get client: {}", id);
    return registrationService
        .withDeadline(Deadline.after(1750, TimeUnit.MILLISECONDS))
        .getClient(
            com.asc.common.application.proto.GetClientRequest.newBuilder().setClientId(id).build());
  }
}
