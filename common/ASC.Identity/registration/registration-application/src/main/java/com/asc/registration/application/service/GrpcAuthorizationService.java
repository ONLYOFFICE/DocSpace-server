// (c) Copyright Ascensio System SIA 2009-2025
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
package com.asc.registration.application.service;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.GetConsentsRequest;
import com.asc.common.application.proto.GetConsentsResponse;
import io.grpc.Deadline;
import java.time.ZonedDateTime;
import java.util.Optional;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Retryable;
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
  @Retryable(
      retryFor = Exception.class,
      maxAttempts = 5,
      backoff = @Backoff(delay = 100, multiplier = 1.625))
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
