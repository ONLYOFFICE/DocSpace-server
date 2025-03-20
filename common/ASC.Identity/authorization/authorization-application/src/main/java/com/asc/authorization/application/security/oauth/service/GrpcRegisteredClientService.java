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
package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.common.application.proto.ClientResponse;
import io.grpc.Deadline;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import net.devh.boot.grpc.client.inject.GrpcClient;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Retryable;
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
  @Retryable(
      retryFor = Exception.class,
      noRetryFor = {RegisteredClientPermissionException.class},
      maxAttempts = 5,
      backoff = @Backoff(delay = 100, multiplier = 1.625))
  public ClientResponse getClient(String id) {
    log.info("GRPC call to get client: {}", id);
    return registrationService
        .withDeadline(Deadline.after(1750, TimeUnit.MILLISECONDS))
        .getClient(
            com.asc.common.application.proto.GetClientRequest.newBuilder().setClientId(id).build());
  }
}
