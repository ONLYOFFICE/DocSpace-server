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

import com.asc.common.application.proto.ClientRegistrationServiceGrpc;
import com.asc.common.application.proto.ClientResponse;
import com.asc.common.application.proto.GetClientRequest;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.google.protobuf.Timestamp;
import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import jakarta.validation.ConstraintViolationException;
import java.time.ZonedDateTime;
import java.util.Collection;
import java.util.List;
import java.util.Objects;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

/**
 * Answers authorization's lookups of a client by its id, for the registration service and for the
 * minified deployment alike.
 *
 * <p>The status of a failed lookup tells authorization what to do about it:
 *
 * <ul>
 *   <li>{@code NOT_FOUND}: the client does not exist;
 *   <li>{@code INVALID_ARGUMENT}: the id is not a valid client id;
 *   <li>{@code UNAVAILABLE}: the lookup could not be completed, so it is worth retrying and counts
 *       against the circuit breaker;
 *   <li>{@code INTERNAL}: the client was found but could not be put into an answer. This concerns
 *       that one client only, so it is neither retried nor counted against the breaker, which would
 *       otherwise stop the lookups of every client.
 * </ul>
 */
@Slf4j
@RequiredArgsConstructor
public abstract class AbstractGrpcClientRegistrationService
    extends ClientRegistrationServiceGrpc.ClientRegistrationServiceImplBase {
  private final ClientApplicationService clientApplicationService;

  private static String orEmpty(String value) {
    return value != null ? value : "";
  }

  private static Collection<String> orEmpty(Collection<String> values) {
    return values != null ? values.stream().filter(Objects::nonNull).toList() : List.of();
  }

  private static Timestamp toTimestamp(ZonedDateTime time) {
    return Timestamp.newBuilder().setSeconds(time.toEpochSecond()).setNanos(time.getNano()).build();
  }

  /**
   * Puts a client into a gRPC answer. Text the client lacks is sent as empty, which is what proto3
   * reads back for an unset field; a missing time is left unset.
   *
   * @param client the client to answer with
   * @return the answer
   */
  static ClientResponse toProto(com.asc.common.service.transfer.response.ClientResponse client) {
    var builder =
        ClientResponse.newBuilder()
            .setClientId(orEmpty(client.getClientId()))
            .setClientSecret(orEmpty(client.getClientSecret()))
            .setDescription(orEmpty(client.getDescription()))
            .setWebsiteUrl(orEmpty(client.getWebsiteUrl()))
            .setTermsUrl(orEmpty(client.getTermsUrl()))
            .setPolicyUrl(orEmpty(client.getPolicyUrl()))
            .addAllAuthenticationMethods(orEmpty(client.getAuthenticationMethods()))
            .setTenant(client.getTenant())
            .addAllRedirectUris(orEmpty(client.getRedirectUris()))
            .addAllAllowedOrigins(orEmpty(client.getAllowedOrigins()))
            .addAllLogoutRedirectUris(orEmpty(client.getLogoutRedirectUri()))
            .addAllScopes(orEmpty(client.getScopes()))
            .setCreatedBy(orEmpty(client.getCreatedBy()))
            .setModifiedBy(orEmpty(client.getModifiedBy()))
            .setEnabled(client.isEnabled())
            .setIsPublic(client.isPublic());
    if (client.getCreatedOn() != null) builder.setCreatedOn(toTimestamp(client.getCreatedOn()));
    if (client.getModifiedOn() != null) builder.setModifiedOn(toTimestamp(client.getModifiedOn()));
    return builder.build();
  }

  /**
   * Fetches client details for the given client ID.
   *
   * @param request the {@link GetClientRequest} containing the client ID.
   * @param responseObserver the {@link StreamObserver} to send the {@link ClientResponse}.
   */
  @Override
  public void getClient(GetClientRequest request, StreamObserver<ClientResponse> responseObserver) {
    var clientId = request.getClientId();
    com.asc.common.service.transfer.response.ClientResponse client;
    try {
      client = clientApplicationService.getStoredClient(clientId);
    } catch (ClientNotFoundException e) {
      responseObserver.onError(
          Status.NOT_FOUND
              .withDescription("Could not find client with id %s".formatted(clientId))
              .asRuntimeException());
      return;
    } catch (IllegalArgumentException | ConstraintViolationException e) {
      responseObserver.onError(
          Status.INVALID_ARGUMENT
              .withDescription("Invalid client id %s".formatted(clientId))
              .asRuntimeException());
      return;
    } catch (Exception e) {
      log.error("Failed to retrieve client with id: {}", clientId, e);
      responseObserver.onError(
          Status.UNAVAILABLE
              .withDescription("Could not retrieve client with id %s".formatted(clientId))
              .asRuntimeException());
      return;
    }

    ClientResponse answer;
    try {
      answer = toProto(client);
    } catch (Exception e) {
      log.error("Failed to build the answer for client with id: {}", clientId, e);
      responseObserver.onError(
          Status.INTERNAL
              .withDescription(
                  "Could not build the answer for client with id %s".formatted(clientId))
              .asRuntimeException());
      return;
    }

    responseObserver.onNext(answer);
    responseObserver.onCompleted();
  }
}
