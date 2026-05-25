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

package com.asc.identity.minified;

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
 * gRPC service implementation for client registration in the minified deployment.
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
