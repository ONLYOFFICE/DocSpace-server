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
      ZonedDateTime lastModifiedAt =
          Optional.of(request.getLastModifiedAt())
              .filter(value -> !value.isBlank())
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
