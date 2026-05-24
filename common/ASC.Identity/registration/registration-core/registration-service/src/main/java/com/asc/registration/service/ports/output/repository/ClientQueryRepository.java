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

package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Optional;

/**
 * Repository interface for querying client-related data.
 *
 * <p>Provides methods for retrieving clients based on various query parameters, including client
 * ID, tenant ID, visibility status, and pagination details.
 */
public interface ClientQueryRepository {

  /**
   * Finds a client by its unique client ID and visibility status.
   *
   * @param clientId the unique client ID.
   * @param visibility the visibility status of the client.
   * @return an {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByIdAndVisibility(ClientId clientId, ClientVisibility visibility);

  /**
   * Finds a client by its unique client ID.
   *
   * @param clientId the unique client ID.
   * @return an {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findById(ClientId clientId);

  /**
   * Finds all public and private clients belonging to a specific tenant created by a specific user,
   * with pagination support.
   *
   * @param tenantId the tenant ID to which the clients belong.
   * @param creatorId the user ID of the creator.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the client cursor for pagination.
   * @param lastCreatedOn the creation timestamp cursor for pagination.
   * @return a {@link PageableResponse} containing the clients for the specified tenant and creator.
   */
  PageableResponse<Client> findAllByTenantIdAndCreatorId(
      TenantId tenantId,
      UserId creatorId,
      int limit,
      String lastClientId,
      ZonedDateTime lastCreatedOn);

  /**
   * Finds all clients belonging to a specific tenant, with pagination support.
   *
   * @param tenantId the tenant ID to which the clients belong.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the client cursor for pagination.
   * @param lastCreatedOn the creation timestamp cursor for pagination.
   * @return a {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllByTenantId(
      TenantId tenantId, int limit, String lastClientId, ZonedDateTime lastCreatedOn);

  /**
   * Finds a client by its unique client ID and tenant ID.
   *
   * @param clientId the unique client ID.
   * @param tenantId the tenant ID to which the client belongs.
   * @return an {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenantId);

  /**
   * Finds a client by its unique client ID, tenant ID, and creator's user ID.
   *
   * @param clientId the unique client ID.
   * @param tenantId the tenant ID to which the client belongs.
   * @param creatorId the user ID of the creator.
   * @return an {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByClientIdAndTenantIdAndCreatorId(
      ClientId clientId, TenantId tenantId, UserId creatorId);

  /**
   * Finds all clients that match the provided list of client IDs.
   *
   * @param clientIds a list of client IDs for which details are to be retrieved.
   * @return a list of {@link Client} objects matching the specified client IDs.
   */
  List<Client> findAllByClientIds(List<ClientId> clientIds);
}
