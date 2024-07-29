// (c) Copyright Ascensio System SIA 2009-2024
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
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Optional;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling client query operations and mapping between domain and data layers.
 * Implements the {@link ClientQueryRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ClientQueryRepositoryDomainAdapter implements ClientQueryRepository {
  private final JpaClientRepository jpaClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;

  /**
   * Finds a client by its ID and visibility.
   *
   * @param clientId the client ID
   * @param visibility the visibility status of the client
   * @return an optional containing the found client, or empty if not found
   */
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
   * @param clientId the client ID
   * @return an optional containing the found client, or empty if not found
   */
  public Optional<Client> findById(ClientId clientId) {
    log.debug("Querying client by client id");
    return jpaClientRepository
        .findById(clientId.getValue().toString())
        .map(clientDataAccessMapper::toDomain);
  }

  /**
   * Finds all public and private clients by tenant ID with pagination.
   *
   * @param tenant the tenant ID
   * @param page the page number
   * @param limit the number of clients per page
   * @return a pageable response containing the clients
   */
  public PageableResponse<Client> findAllPublicAndPrivateByTenantId(
      TenantId tenant, int page, int limit) {
    log.debug("Querying all public and private clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllPublicAndPrivateByTenant(
            tenant.getValue(), Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<Client>builder()
            .page(page)
            .limit(limit)
            .data(
                clients.stream()
                    .filter(c -> !c.isInvalidated())
                    .map(clientDataAccessMapper::toDomain)
                    .collect(Collectors.toSet()));

    if (clients.hasPrevious()) builder.previous(page - 1);
    if (clients.hasNext()) builder.next(page + 1);

    return builder.build();
  }

  /**
   * Finds all clients by tenant ID with pagination.
   *
   * @param tenant the tenant ID
   * @param page the page number
   * @param limit the number of clients per page
   * @return a pageable response containing the clients
   */
  public PageableResponse<Client> findAllByTenantId(TenantId tenant, int page, int limit) {
    log.debug("Querying clients by tenant id with pagination");
    var clients =
        jpaClientRepository.findAllByTenantId(
            tenant.getValue(), Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<Client>builder()
            .page(page)
            .limit(limit)
            .data(
                clients.stream()
                    .filter(c -> !c.isInvalidated())
                    .map(clientDataAccessMapper::toDomain)
                    .collect(Collectors.toSet()));

    if (clients.hasPrevious()) builder.previous(page - 1);
    if (clients.hasNext()) builder.next(page + 1);

    return builder.build();
  }

  /**
   * Finds a client by its client ID and tenant ID.
   *
   * @param clientId the client ID
   * @param tenant the tenant ID
   * @return an optional containing the found client, or empty if not found
   */
  public Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant) {
    log.debug("Querying client by client id and tenant id");
    return jpaClientRepository
        .findClientByClientIdAndTenantId(clientId.getValue().toString(), tenant.getValue())
        .map(clientDataAccessMapper::toDomain);
  }
}
