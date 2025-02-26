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
import com.asc.common.core.domain.value.Role;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
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

/**
 * Handles queries related to client information retrieval.
 *
 * <p>This component provides methods to obtain both detailed and basic client information.
 * Role-based restrictions are applied: tenant admins have broader access, while non-admin users are
 * limited to clients they have created or that are publicly visible.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientQueryHandler {
  private final ClientDataMapper clientDataMapper;
  private final ClientQueryRepository clientQueryRepository;
  private final EncryptionService encryptionService;

  /**
   * Retrieves detailed information about a client using tenant and client identifiers.
   *
   * <p>For tenant admins (ROLE_ADMIN), the method returns client details based solely on the tenant
   * and client IDs. For non-admin roles, it additionally verifies that the client was created by
   * the requesting user.
   *
   * @param role the role of the requesting user (e.g., ROLE_ADMIN or a non-admin role)
   * @param query the query object containing the tenant ID, client ID, and for non-admin users, the
   *     user ID
   * @return a {@link ClientResponse} containing the detailed client information, including a
   *     decrypted client secret
   * @throws ClientNotFoundException if no matching client is found for the given tenant (and user,
   *     if applicable)
   */
  public ClientResponse getClient(Role role, TenantClientQuery query) {
    log.info(
        "Retrieving client details for client ID: {} and tenant ID: {} with role: {}",
        query.getClientId(),
        query.getTenantId(),
        role.name());

    if (role.equals(Role.ROLE_GUEST))
      throw new ClientNotFoundException(
          String.format(
              "Client with ID %s for tenant %s was not found",
              query.getClientId(), query.getTenantId()));

    var client =
        role.equals(Role.ROLE_ADMIN)
            ? (clientQueryRepository
                .findByClientIdAndTenantId(
                    new ClientId(UUID.fromString(query.getClientId())),
                    new TenantId(query.getTenantId()))
                .orElseThrow(
                    () ->
                        new ClientNotFoundException(
                            String.format(
                                "Client with ID %s for tenant %s was not found",
                                query.getClientId(), query.getTenantId()))))
            : (clientQueryRepository
                .findByClientIdAndTenantIdAndCreatorId(
                    new ClientId(UUID.fromString(query.getClientId())),
                    new TenantId(query.getTenantId()),
                    new UserId(query.getUserId()))
                .orElseThrow(
                    () ->
                        new ClientNotFoundException(
                            String.format(
                                "Client with ID %s for tenant %s and user %s was not found",
                                query.getClientId(), query.getTenantId(), query.getUserId()))));

    return decryptAndMapClientResponse(client);
  }

  /**
   * Retrieves detailed client information based solely on the client identifier.
   *
   * <p>This method bypasses tenant and creator checks, assuming that the client ID is sufficient to
   * uniquely identify the client.
   *
   * @param clientId the unique client identifier as a string
   * @return a {@link ClientResponse} containing the detailed client information, including a
   *     decrypted client secret
   * @throws ClientNotFoundException if no client exists with the provided ID
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
   * Decrypts sensitive client information and maps the client entity to a response DTO.
   *
   * <p>Specifically, this method decrypts the client secret before returning the response.
   *
   * @param client the client entity to process
   * @return a {@link ClientResponse} object with decrypted sensitive data
   */
  private ClientResponse decryptAndMapClientResponse(Client client) {
    var response = clientDataMapper.toClientResponse(client);
    log.info("Decrypting client secret for client ID: {}", client.getId().getValue());
    response.setClientSecret(encryptionService.decrypt(response.getClientSecret()));
    return response;
  }

  /**
   * Retrieves basic client information using tenant and client identifiers.
   *
   * <p>For tenant admins, the client information is retrieved based on tenant and client IDs. For
   * non-admin users, the query further checks that the client was created by the requesting user.
   * Additionally, if the client does not belong to the tenant specified in the query and is not
   * public, access is denied.
   *
   * @param role the role of the requesting user (e.g., ROLE_ADMIN or a non-admin role)
   * @param query the query object containing the client ID, tenant ID, and for non-admin users, the
   *     user ID
   * @return a {@link ClientInfoResponse} containing basic client details
   * @throws ClientNotFoundException if the client is not found or access is denied due to
   *     visibility restrictions
   */
  public ClientInfoResponse getClientInfo(Role role, ClientInfoQuery query) {
    log.info(
        "Retrieving client basic information by client id: {} and role: {}",
        query.getClientId(),
        role.name());

    if (role.equals(Role.ROLE_GUEST))
      throw new ClientNotFoundException(
          String.format(
              "Client with ID %s for tenant %s was not found",
              query.getClientId(), query.getTenantId()));

    var client =
        role.equals(Role.ROLE_ADMIN)
            ? (clientQueryRepository
                .findByClientIdAndTenantId(
                    new ClientId(UUID.fromString(query.getClientId())),
                    new TenantId(query.getTenantId()))
                .orElseThrow(
                    () ->
                        new ClientNotFoundException(
                            String.format(
                                "Client with ID %s for tenant %s was not found",
                                query.getClientId(), query.getTenantId()))))
            : (clientQueryRepository
                .findByClientIdAndTenantIdAndCreatorId(
                    new ClientId(UUID.fromString(query.getClientId())),
                    new TenantId(query.getTenantId()),
                    new UserId(query.getUserId()))
                .orElseThrow(
                    () ->
                        new ClientNotFoundException(
                            String.format(
                                "Client with ID %s for tenant %s and user %s was not found",
                                query.getClientId(), query.getTenantId(), query.getUserId()))));

    var clientTenant = client.getClientTenantInfo().tenantId().getValue();
    var clientVisibility = client.getVisibility();
    if (!clientTenant.equals(query.getTenantId())
        && !clientVisibility.equals(ClientVisibility.PUBLIC))
      throw new ClientNotFoundException(
          String.format(
              "Client with id %s can't be accessed by current user", query.getClientId()));

    return clientDataMapper.toClientInfoResponse(client);
  }

  /**
   * Retrieves a pageable list of basic client information for a given tenant.
   *
   * <p>For tenant admins, the method returns all clients within the tenant. For non-admin users, it
   * only returns clients created by the requesting user. Pagination is supported via the provided
   * limit and cursor parameters.
   *
   * @param role the role of the requesting user (e.g., ROLE_ADMIN or a non-admin role)
   * @param query the pagination query object containing tenant ID, limit, and cursor parameters
   *     such as the last client ID and creation timestamp
   * @return a {@link PageableResponse} containing a set of {@link ClientInfoResponse} and
   *     pagination metadata
   */
  public PageableResponse<ClientInfoResponse> getClientsInfo(
      Role role, ClientInfoPaginationQuery query) {
    log.info("Retrieving clients pageable information with role: {}", role.name());

    if (role.equals(Role.ROLE_GUEST))
      return PageableResponse.<ClientInfoResponse>builder()
          .data(Set.of())
          .lastClientId(null)
          .lastCreatedOn(null)
          .limit(query.getLimit())
          .build();

    var result =
        role.equals(Role.ROLE_ADMIN)
            ? clientQueryRepository.findAllByTenantId(
                new TenantId(query.getTenantId()),
                query.getLimit(),
                query.getLastClientId(),
                query.getLastCreatedOn())
            : clientQueryRepository.findAllByTenantIdAndCreatorId(
                new TenantId(query.getTenantId()),
                new UserId(query.getUserId()),
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
   * Retrieves basic client information based solely on the client identifier.
   *
   * <p>This method provides a lightweight version of client details without tenant or creator
   * verification.
   *
   * @param clientId the unique client identifier as a string
   * @return a {@link ClientInfoResponse} containing basic client details
   * @throws ClientNotFoundException if no client exists with the provided ID
   */
  public ClientInfoResponse getClientInfo(String clientId) {
    log.info("Retrieving client basic information by client id: {}", clientId);

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
   * Retrieves a pageable list of detailed client information for a specific tenant.
   *
   * <p>For tenant admins, this method returns all clients within the tenant. For non-admin users,
   * it only returns clients that were created by the requesting user. Pagination is controlled
   * using the provided limit and cursor parameters.
   *
   * @param role the role of the requesting user (e.g., ROLE_ADMIN or a non-admin role)
   * @param query the pagination query object containing tenant ID, limit, and cursor parameters
   *     (last client ID and last created timestamp)
   * @return a {@link PageableResponse} containing a set of {@link ClientResponse} and pagination
   *     metadata
   */
  public PageableResponse<ClientResponse> getClients(
      Role role, TenantClientsPaginationQuery query) {
    log.info(
        "Retrieving all clients by tenant id: {} with role: {}", query.getTenantId(), role.name());

    if (role.equals(Role.ROLE_GUEST))
      return PageableResponse.<ClientResponse>builder()
          .data(Set.of())
          .lastClientId(null)
          .lastCreatedOn(null)
          .limit(query.getLimit())
          .build();

    var result =
        role.equals(Role.ROLE_ADMIN)
            ? clientQueryRepository.findAllByTenantId(
                new TenantId(query.getTenantId()),
                query.getLimit(),
                query.getLastClientId(),
                query.getLastCreatedOn())
            : clientQueryRepository.findAllByTenantIdAndCreatorId(
                new TenantId(query.getTenantId()),
                new UserId(query.getUserId()),
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
   * Retrieves detailed client information for a list of client identifiers.
   *
   * <p>This method processes a collection of {@link ClientId} objects and returns a corresponding
   * list of detailed {@link ClientResponse} objects.
   *
   * @param clientIds a list of {@link ClientId} objects for which detailed information is requested
   * @return a list of {@link ClientResponse} objects containing detailed client information
   */
  public List<ClientResponse> getClients(List<ClientId> clientIds) {
    log.info("Retrieving client details for a list of clients");

    var result = clientQueryRepository.findAllByClientIds(clientIds);
    return result.stream().map(clientDataMapper::toClientResponse).toList();
  }
}
