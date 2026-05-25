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

package com.asc.authorization.container.service;

import com.asc.authorization.application.security.oauth.service.AuthorizationCleanupService;
import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.Consent;
import com.asc.common.application.proto.GetConsentsRequest;
import com.asc.common.application.proto.GetConsentsResponse;
import com.asc.common.application.proto.RevokeConsentsRequest;
import com.asc.common.application.proto.RevokeConsentsResponse;
import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import java.time.ZonedDateTime;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.Optional;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import net.devh.boot.grpc.server.service.GrpcService;
import org.apache.logging.log4j.util.Strings;

/**
 * gRPC service implementation for managing authorization consents.
 *
 * <p>This service provides gRPC endpoints to revoke and retrieve consent data associated with a
 * particular principal and client.
 */
@GrpcService
@RequiredArgsConstructor
public class GrpcAuthorizationService
    extends AuthorizationServiceGrpc.AuthorizationServiceImplBase {
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final AuthorizationCleanupService cleanupService;

  /**
   * Revokes consents for a given principal and client.
   *
   * @param request the {@link RevokeConsentsRequest} containing the principal ID and client ID for
   *     which consents should be revoked.
   * @param responseObserver the {@link StreamObserver} used to send the {@link
   *     RevokeConsentsResponse}.
   */
  public void revokeConsents(
      RevokeConsentsRequest request, StreamObserver<RevokeConsentsResponse> responseObserver) {
    try {
      cleanupService.remove(request.getPrincipalId(), request.getClientId());
      responseObserver.onNext(RevokeConsentsResponse.newBuilder().setSuccess(true).build());
      responseObserver.onCompleted();
    } catch (Exception e) {
      responseObserver.onError(
          Status.INTERNAL
              .withDescription("Could not revoke consents for client: " + request.getClientId())
              .asRuntimeException());
    }
  }

  /**
   * Retrieves a list of consents for a given principal, optionally filtered by a last modified
   * date.
   *
   * @param request the {@link GetConsentsRequest} containing the principal ID, optional last
   *     modified date (as a String), and the number of results to limit.
   * @param responseObserver the {@link StreamObserver} used to send the {@link
   *     GetConsentsResponse}.
   */
  public void getConsents(
      GetConsentsRequest request, StreamObserver<GetConsentsResponse> responseObserver) {
    try {
      var lastModifiedAt =
          Optional.of(request.getLastModifiedAt())
              .filter(v -> !v.isBlank())
              .map(ZonedDateTime::parse)
              .orElse(null);
      var authorizations =
          jpaAuthorizationRepository.findConsentedAuthorizationsByPrincipalId(
              request.getPrincipalId(), lastModifiedAt, request.getLimit() + 1);

      var lastClient =
          authorizations.size() > request.getLimit()
              ? authorizations.get(request.getLimit() - 1)
              : null;

      var responseBuilder =
          GetConsentsResponse.newBuilder()
              .addAllConsents(
                  authorizations.stream()
                      .limit(request.getLimit())
                      .map(
                          a ->
                              Consent.newBuilder()
                                  .setClientId(a.getRegisteredClientId())
                                  .addAllScopes(
                                      Arrays.stream(a.getAuthorizedScopes().split(",")).toList())
                                  .setModifiedAt(
                                      a.getModifiedAt() != null
                                          ? a.getModifiedAt().toString()
                                          : Strings.EMPTY)
                                  .build())
                      .collect(Collectors.toCollection(LinkedHashSet::new)));

      if (lastClient != null && lastClient.getModifiedAt() != null)
        responseBuilder.setLastModifiedAt(lastClient.getModifiedAt().toString());

      responseObserver.onNext(responseBuilder.build());
      responseObserver.onCompleted();
    } catch (Exception e) {
      responseObserver.onError(
          Status.INTERNAL
              .withDescription(
                  "Could not fetch consents for principal: " + request.getPrincipalId())
              .asRuntimeException());
    }
  }
}
