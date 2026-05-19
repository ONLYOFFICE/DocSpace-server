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

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.GetConsentsRequest;
import com.asc.common.application.proto.GetConsentsResponse;
import io.github.resilience4j.retry.annotation.Retry;
import io.grpc.Deadline;
import java.time.ZonedDateTime;
import java.util.Optional;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

/**
 * Service responsible for communicating with the remote Authorization Service via gRPC.
 *
 * <p>This service encapsulates the logic required to invoke the gRPC endpoint for retrieving
 * consent records. It applies a deadline for the remote call and includes retry logic to handle
 * transient failures.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class GrpcAuthorizationService {
  private final AuthorizationServiceGrpc.AuthorizationServiceBlockingStub authorizationService;

  /**
   * Retrieves consent records for a specified principal by performing a gRPC call.
   *
   * <p>This method builds a {@link GetConsentsRequest} with the given principal identifier, limit,
   * and an optional last modified timestamp. The gRPC call is configured with a deadline of 1750
   * milliseconds and will be retried up to 5 times in the event of a failure.
   *
   * @param principalId the unique identifier of the principal whose consents are requested.
   * @param limit the maximum number of consent records to retrieve.
   * @param lastModifiedOn an optional timestamp to filter consents, returning only those modified
   *     after this time. May be {@code null} to indicate no filtering.
   * @return a {@link GetConsentsResponse} containing the list of consent records along with
   *     associated metadata.
   * @throws Exception if the gRPC call fails after exhausting the configured retry attempts.
   */
  @Retry(name = "grpcAuthorizationRetry")
  public GetConsentsResponse getConsents(
      String principalId, int limit, ZonedDateTime lastModifiedOn) {
    log.info("GRPC call to get principal {} consents", principalId);
    return authorizationService
        .withDeadline(Deadline.after(1750, TimeUnit.MILLISECONDS))
        .getConsents(
            GetConsentsRequest.newBuilder()
                .setPrincipalId(principalId)
                .setLimit(limit)
                .setLastModifiedAt(
                    Optional.ofNullable(lastModifiedOn).map(ZonedDateTime::toString).orElse(""))
                .build());
  }
}
