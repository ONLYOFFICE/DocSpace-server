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

package com.asc.registration.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

/** Handles client-related queries. */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientQueryHandler {
  private final ClientDataMapper clientDataMapper;
  private final ClientQueryRepository clientQueryRepository;
  private final EncryptionService encryptionService;

  /**
   * Retrieves detailed information about a client by tenant ID and client ID. Should only be
   * accessed by tenant admins.
   *
   * @param query The query containing tenant ID and client ID.
   * @return A {@link ClientResponse} containing the detailed client information.
   * @throws ClientNotFoundException if the client is not found.
   */
  public ClientResponse getClient(TenantClientQuery query) {
    log.info(
        "Retrieving client details for client ID: {} and tenant ID: {}",
        query.getClientId(),
        query.getTenantId());

    var client =
        clientQueryRepository
            .findByClientIdAndTenantId(
                new ClientId(UUID.fromString(query.getClientId())),
                new TenantId(query.getTenantId()))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format(
                            "Client with ID %s for tenant %s was not found",
                            query.getClientId(), query.getTenantId())));

    return decryptAndMapClientResponse(client);
  }

  /**
   * Retrieves detailed information about a client by client ID.
   *
   * @param clientId The unique identifier of the client.
   * @return A {@link ClientResponse} containing the detailed client information.
   * @throws ClientNotFoundException if the client is not found.
   */
  public ClientResponse getClient(String clientId) {
    log.info("Retrieving client details for client ID: {}", clientId);

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(clientId)))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with ID %s was not found", clientId)));

    return decryptAndMapClientResponse(client);
  }

  /**
   * Decrypts sensitive client information and maps the client entity to a response.
   *
   * @param client The client entity to process.
   * @return A {@link ClientResponse} with decrypted and mapped information.
   */
  private ClientResponse decryptAndMapClientResponse(Client client) {
    var response = clientDataMapper.toClientResponse(client);
    log.info("Decrypting client secret for client ID: {}", client.getId().getValue());
    response.setClientSecret(encryptionService.decrypt(response.getClientSecret()));
    return response;
  }

  /**
   * Retrieves basic information of a client by client ID and tenant ID.
   *
   * @param query the query containing client ID
   * @return the response containing client basic information
   */
  public ClientInfoResponse getClientInfo(ClientInfoQuery query) {
    log.info("Trying to get client basic information by client id");

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(query.getClientId())))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with id %s was not found", query.getClientId())));

    var clientTenant = client.getClientTenantInfo().tenantId().getValue();
    var clientVisibility = client.getVisibility();
    if (!clientTenant.equals(query.getTenantId())
        && !clientVisibility.equals(ClientVisibility.PUBLIC))
      throw new ClientNotFoundException(
          String.format(
              "Client with id %s can't be accessed by current user", query.getClientId()));

    return clientDataMapper.toClientInfoResponse(client);
  }

  public PageableResponse<ClientInfoResponse> getClientsInfo(ClientInfoPaginationQuery query) {
    log.info("Trying to get clients information by client id");

    var result =
        clientQueryRepository.findAllPublicAndPrivateByTenantId(
            new TenantId(query.getTenantId()),
            query.getLimit(),
            query.getLastClientId(),
            query.getLastCreatedOn());

    if (result == null)
      return PageableResponse.<ClientInfoResponse>builder()
          .data(Set.of())
          .lastClientId(null)
          .lastCreatedOn(null)
          .limit(query.getLimit())
          .build();

    return PageableResponse.<ClientInfoResponse>builder()
        .lastClientId(result.getLastClientId())
        .lastCreatedOn(result.getLastCreatedOn())
        .limit(result.getLimit())
        .data(
            StreamSupport.stream(result.getData().spliterator(), false)
                .map(clientDataMapper::toClientInfoResponse)
                .collect(Collectors.toCollection(LinkedHashSet::new)))
        .build();
  }

  /**
   * Retrieves basic information of a client by client ID.
   *
   * @param clientId the query containing client ID
   * @return the response containing client basic information
   */
  public ClientInfoResponse getClientInfo(String clientId) {
    log.info("Trying to get client basic information by client id");

    var client =
        clientQueryRepository
            .findById(new ClientId(UUID.fromString(clientId)))
            .orElseThrow(
                () ->
                    new ClientNotFoundException(
                        String.format("Client with id %s was not found", clientId)));

    return clientDataMapper.toClientInfoResponse(client);
  }

  /**
   * Retrieves all clients for a tenant with pagination. Should be only accessed by tenant admins.
   *
   * @param query the query containing tenant ID, page, and limit
   * @return the pageable response containing client details
   */
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    log.info("Trying to get all clients by tenant id");

    var result =
        clientQueryRepository.findAllByTenantId(
            new TenantId(query.getTenantId()),
            query.getLimit(),
            query.getLastClientId(),
            query.getLastCreatedOn());

    if (result == null)
      return PageableResponse.<ClientResponse>builder()
          .data(Set.of())
          .lastClientId(null)
          .lastCreatedOn(null)
          .limit(query.getLimit())
          .build();

    var data =
        StreamSupport.stream(result.getData().spliterator(), false)
            .map(clientDataMapper::toClientResponse)
            .collect(Collectors.toCollection(LinkedHashSet::new));

    return PageableResponse.<ClientResponse>builder()
        .lastClientId(result.getLastClientId())
        .lastCreatedOn(result.getLastCreatedOn())
        .limit(result.getLimit())
        .data(data)
        .build();
  }

  /**
   * Retrieves client details for a list of client IDs.
   *
   * @param clientIds a list of client IDs for which details are to be retrieved.
   * @return a list of {@link ClientResponse} containing client details.
   */
  public List<ClientResponse> getClients(List<ClientId> clientIds) {
    log.info("Fetching client details for a list of clients");

    var result = clientQueryRepository.findAllByClientIds(clientIds);
    return result.stream().map(clientDataMapper::toClientResponse).toList();
  }
}
