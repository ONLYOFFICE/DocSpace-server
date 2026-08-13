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

import com.asc.authorization.application.exception.client.GrpcDeadlineExceededException;
import com.asc.authorization.application.exception.client.NonRetryableGrpcException;
import com.asc.common.application.proto.ClientResponse;
import io.github.resilience4j.circuitbreaker.annotation.CircuitBreaker;
import io.github.resilience4j.retry.annotation.Retry;
import io.grpc.Deadline;
import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import java.util.concurrent.TimeUnit;
import lombok.extern.slf4j.Slf4j;
import net.devh.boot.grpc.client.inject.GrpcClient;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

/**
 * Service for interacting with the gRPC client registration service.
 *
 * <p>This service provides methods to retrieve client information from a gRPC service. It includes
 * retry logic for handling transient errors.
 */
@Slf4j
@Service
public class GrpcRegisteredClientService {
  /** The gRPC client stub for the registration service. */
  @GrpcClient("registrationService")
  com.asc.common.application.proto.ClientRegistrationServiceGrpc
          .ClientRegistrationServiceBlockingStub
      registrationService;

  private final long deadlineMs;

  public GrpcRegisteredClientService(
      @Value("${GRPC_CLIENT_REGISTRATION_DEADLINE_MS:400}") long deadlineMs) {
    this.deadlineMs = deadlineMs;
  }

  private static boolean isNonRetryable(Status.Code code) {
    return switch (code) {
      case NOT_FOUND,
          INVALID_ARGUMENT,
          PERMISSION_DENIED,
          UNAUTHENTICATED,
          FAILED_PRECONDITION,
          ALREADY_EXISTS,
          OUT_OF_RANGE,
          UNIMPLEMENTED ->
          true;
      default -> false;
    };
  }

  /**
   * Retrieves a client by its ID from the gRPC service.
   *
   * <p>Uses a short deadline, retries once on transient gRPC errors, and trips a circuit breaker
   * when Registration is down so later calls fail immediately. Client-fault codes such as {@code
   * NOT_FOUND} are not retried and do not open the breaker. {@code DEADLINE_EXCEEDED} is not
   * retried but is recorded as a breaker failure.
   *
   * @param id the ID of the client to retrieve.
   * @return the {@link ClientResponse} containing the client information.
   */
  @CircuitBreaker(name = "grpcClientCircuitBreaker")
  @Retry(name = "grpcClientRetry")
  public ClientResponse getClient(String id) {
    log.info("GRPC call to get client: {}", id);
    try {
      return registrationService
          .withDeadline(Deadline.after(deadlineMs, TimeUnit.MILLISECONDS))
          .getClient(
              com.asc.common.application.proto.GetClientRequest.newBuilder()
                  .setClientId(id)
                  .build());
    } catch (StatusRuntimeException e) {
      if (e.getStatus().getCode() == Status.Code.DEADLINE_EXCEEDED)
        throw new GrpcDeadlineExceededException(e);
      if (isNonRetryable(e.getStatus().getCode())) throw new NonRetryableGrpcException(e);
      throw e;
    }
  }
}
