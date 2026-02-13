// (c) Copyright Ascensio System SIA 2009-2026
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

package com.asc.registration.messaging.listener;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.service.transfer.message.ClientRetrievedEvent;
import com.asc.common.service.transfer.message.RetrieveClientMessage;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import java.util.List;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

/**
 * RabbitMQ RPC listener for cross-region client retrieval requests.
 *
 * <p>This component handles RPC requests to fetch client information from a remote region during
 * token introspection. It is only active in the "saas" profile for multi-region deployments.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@Profile("saas")
@RabbitListener(
    queues = "asc_identity_client_rpc_${spring.application.region}_queue",
    containerFactory = "rabbitRpcContainerFactory")
public class ClientRegistrationMessagingRPCListener {
  private final ClientQueryRepository clientQueryRepository;
  private final ClientDataMapper clientMapper;

  /**
   * Builds a {@link ClientRetrievedEvent} with only essential OAuth2 fields.
   *
   * <p>Excludes unnecessary fields like logo, descriptions, URLs, timestamps, tenant info to
   * minimize RPC message size. The client secret is included in encrypted form.
   *
   * @param response the full client response from the repository
   * @return the minimal client event for RPC transmission
   */
  private ClientRetrievedEvent buildClientRetrievedEvent(ClientResponse response) {
    return ClientRetrievedEvent.builder()
        .clientId(response.getClientId())
        .name(response.getName())
        .clientSecret(response.getClientSecret())
        .authenticationMethods(
            response.getAuthenticationMethods() != null
                ? List.copyOf(response.getAuthenticationMethods())
                : List.of())
        .redirectUris(
            response.getRedirectUris() != null
                ? List.copyOf(response.getRedirectUris())
                : List.of())
        .scopes(response.getScopes() != null ? List.copyOf(response.getScopes()) : List.of())
        .build();
  }

  /**
   * Handles client retrieval RPC requests from remote regions.
   *
   * <p>Returns essential OAuth2 client information with the client secret in encrypted form.
   *
   * @param request the message containing the client ID to retrieve
   * @return the {@link ClientRetrievedEvent} with client information (secret encrypted), or {@code
   *     null} if not found
   */
  @RabbitHandler
  public ClientRetrievedEvent receiveClientRetrieval(RetrieveClientMessage request) {
    log.info("Received client retrieval request for ID: {}", request.getClientId());

    try {
      var clientId = new ClientId(UUID.fromString(request.getClientId()));
      var client =
          clientQueryRepository
              .findById(clientId)
              .orElseThrow(
                  () -> new IllegalArgumentException("Client not found: " + request.getClientId()));

      log.info("Found client: {}", request.getClientId());

      return buildClientRetrievedEvent(clientMapper.toClientResponse(client));
    } catch (Exception e) {
      log.warn("Failed to retrieve client with ID: {}", request.getClientId(), e);
      return null;
    }
  }
}
