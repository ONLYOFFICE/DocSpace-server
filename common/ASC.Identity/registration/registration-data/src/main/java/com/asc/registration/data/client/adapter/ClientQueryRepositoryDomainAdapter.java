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

package com.asc.registration.data.client.adapter;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.client.repository.JpaClientRepository;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.time.ZonedDateTime;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

/**
 * Adapter class for handling client query operations and mapping between domain and data layers.
 * Implements the {@link ClientQueryRepository} interface, providing database access logic for
 * querying client entities using a JPA repository.
 */
@Slf4j
@Repository
@Profile("!saas")
@RequiredArgsConstructor
public class ClientQueryRepositoryDomainAdapter implements ClientQueryRepository {
  private final JpaClientRepository jpaClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;

  /**
   * Finds a client by its ID and visibility.
   *
   * @param clientId the unique identifier of the client
   * @param visibility the visibility status of the client (e.g., PUBLIC or PRIVATE)
   * @return an {@link Optional} containing the found client if it exists, or empty otherwise
   */
  @Transactional(timeout = 2, readOnly = true)
  public Optional<Client> findByIdAndVisibility(ClientId clientId, ClientVisibility visibility) {
    log.debug("Querying client by client id and visibility");
    return jpaClientRepository
        .findByIdAndVisibility(
            clientId.getValue().toString(), visibility.equals(ClientVisibility.PUBLIC))
        .map(clientDataAccessMapper::toDomain);
  }

  /**
   * Finds a client by its ID.
   *
   * @param clientId the unique identifier of the client
   * @return an {@link Optional} containing the found client if it exists, or empty otherwise
   */
  @Transactional(timeout = 2, readOnly = true)
  public Optional<Client> findById(ClientId clientId) {
    log.debug("Querying client by client id");
    var response =
        jpaClientRepository
            .findById(clientId.getValue().toString())
            .map(clientDataAccessMapper::toDomain);
    return response;
  }

  /**
   * Finds all public and private clients associated with a tenant ID, with pagination support.
   *
   * @param tenant the tenant ID
   * @param limit the maximum number of clients to retrieve
   * @param lastClientId the ID of the last client retrieved in the previous page (for cursor-based
   *     pagination)
   * @param lastCreatedOn the creation timestamp of the last client retrieved in the previous page
   * @return a {@link PageableResponse} containing the retrieved clients and pagination metadata
   */
  @Transactional(timeout = 3, readOnly = true)
  public PageableResponse<Client> findAllPublicAndPrivateByTenantId(
      TenantId tenant, int limit, String lastClientId, ZonedDateTime lastCreatedOn) {
    log.debug("Querying all public and private clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllPublicAndPrivateByTenantWithCursor(
            tenant.getValue(), lastCreatedOn, limit + 1);
    var lastClient = clients.size() > limit ? clients.get(limit - 1) : null;

    var data =
        clients.stream()
            .limit(limit)
            .filter(c -> !c.isInvalidated())
            .map(clientDataAccessMapper::toDomain)
            .collect(Collectors.toCollection(LinkedHashSet::new));

    var builder =
        PageableResponse.<Client>builder()
            .lastClientId(lastClient == null ? null : lastClient.getClientId())
            .lastCreatedOn(lastClient == null ? null : lastClient.getCreatedOn())
            .limit(limit)
            .data(data);

    return builder.build();
  }

  /**
   * Finds all clients associated with a tenant ID, with pagination support.
   *
   * @param tenant the tenant ID
   * @param limit the maximum number of clients to retrieve
   * @param lastClientId the ID of the last client retrieved in the previous page (for cursor-based
   *     pagination)
   * @param lastCreatedOn the creation timestamp of the last client retrieved in the previous page
   * @return a {@link PageableResponse} containing the retrieved clients and pagination metadata
   */
  @Transactional(timeout = 3, readOnly = true)
  public PageableResponse<Client> findAllByTenantId(
      TenantId tenant, int limit, String lastClientId, ZonedDateTime lastCreatedOn) {
    log.debug("Querying clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllByTenantIdWithCursor(
            tenant.getValue(), lastCreatedOn, limit + 1);
    var lastClient = clients.size() > limit ? clients.get(limit - 1) : null;

    var data =
        clients.stream()
            .limit(limit)
            .filter(c -> !c.isInvalidated())
            .map(clientDataAccessMapper::toDomain)
            .collect(Collectors.toCollection(LinkedHashSet::new));

    var builder =
        PageableResponse.<Client>builder()
            .lastClientId(lastClient == null ? null : lastClient.getClientId())
            .lastCreatedOn(lastClient == null ? null : lastClient.getCreatedOn())
            .limit(limit)
            .data(data);

    return builder.build();
  }

  /**
   * Finds a client by its client ID and tenant ID.
   *
   * @param clientId the unique identifier of the client
   * @param tenant the tenant ID associated with the client
   * @return an {@link Optional} containing the found client if it exists, or empty otherwise
   */
  @Transactional(timeout = 2, readOnly = true)
  public Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant) {
    log.debug("Querying client by client id and tenant id");
    return jpaClientRepository
        .findByClientIdAndTenantId(clientId.getValue().toString(), tenant.getValue())
        .map(clientDataAccessMapper::toDomain);
  }

  /**
   * Finds all clients by a list of client IDs.
   *
   * @param clientIds a list of client IDs to query
   * @return a {@link List} of clients corresponding to the provided IDs
   */
  @Transactional(timeout = 2, readOnly = true)
  public List<Client> findAllByClientIds(List<ClientId> clientIds) {
    log.debug("Querying all clients by client ids");
    return jpaClientRepository
        .findAllByClientIds(clientIds.stream().map(i -> i.getValue().toString()).toList())
        .stream()
        .map(clientDataAccessMapper::toDomain)
        .toList();
  }
}
