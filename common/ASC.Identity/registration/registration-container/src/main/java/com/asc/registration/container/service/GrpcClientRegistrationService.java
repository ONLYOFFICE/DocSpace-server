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

package com.asc.registration.container.service;

import com.asc.common.application.proto.ClientRegistrationServiceGrpc;
import com.asc.common.application.proto.ClientResponse;
import com.asc.common.application.proto.GetClientRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.google.protobuf.Timestamp;
import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import lombok.RequiredArgsConstructor;
import net.devh.boot.grpc.server.service.GrpcService;

/**
 * gRPC service implementation for client registration.
 *
 * <p>This service provides a gRPC endpoint to fetch client details by client ID. It extends {@link
 * ClientRegistrationServiceGrpc.ClientRegistrationServiceImplBase} to integrate with gRPC
 * infrastructure and handle requests.
 */
@GrpcService
@RequiredArgsConstructor
public class GrpcClientRegistrationService
    extends ClientRegistrationServiceGrpc.ClientRegistrationServiceImplBase {

  private final ClientApplicationService clientApplicationService;

  /**
   * Fetches client details for the given client ID.
   *
   * <p>Constructs a {@link ClientResponse} containing the client's details and sends it back to the
   * caller. If the client is not found, an appropriate error is returned.
   *
   * @param request the {@link GetClientRequest} containing the client ID.
   * @param responseObserver the {@link StreamObserver} to send the {@link ClientResponse}.
   */
  public void getClient(GetClientRequest request, StreamObserver<ClientResponse> responseObserver) {
    try {
      var client = clientApplicationService.getClient(request.getClientId());
      responseObserver.onNext(
          ClientResponse.newBuilder()
              .setClientId(client.getClientId())
              .setClientSecret(client.getClientSecret())
              .setDescription(client.getDescription())
              .setWebsiteUrl(client.getWebsiteUrl())
              .setTermsUrl(client.getTermsUrl())
              .setPolicyUrl(client.getPolicyUrl())
              .addAllAuthenticationMethods(client.getAuthenticationMethods())
              .setTenant(client.getTenant())
              .addAllRedirectUris(client.getRedirectUris())
              .addAllAllowedOrigins(client.getAllowedOrigins())
              .addAllLogoutRedirectUris(client.getLogoutRedirectUri())
              .addAllScopes(client.getScopes())
              .setCreatedOn(
                  Timestamp.newBuilder()
                      .setSeconds(client.getCreatedOn().getSecond())
                      .setNanos(client.getCreatedOn().getNano())
                      .build())
              .setCreatedBy(client.getCreatedBy())
              .setModifiedOn(
                  Timestamp.newBuilder()
                      .setSeconds(client.getModifiedOn().getSecond())
                      .setNanos(client.getModifiedOn().getNano())
                      .build())
              .setModifiedBy(client.getModifiedBy())
              .setEnabled(client.isEnabled())
              .setIsPublic(client.isPublic())
              .build());
      responseObserver.onCompleted();
    } catch (Exception e) {
      responseObserver.onError(
          Status.NOT_FOUND
              .withDescription("Could not find client with id " + request.getClientId())
              .asRuntimeException());
    }
  }
}
