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

package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
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
   * Finds all public and private clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant the tenant ID to which the clients belong.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the client cursor for pagination.
   * @param lastCreatedOn the creation timestamp cursor for pagination.
   * @return a {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllPublicAndPrivateByTenantId(
      TenantId tenant, int limit, String lastClientId, ZonedDateTime lastCreatedOn);

  /**
   * Finds all clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant the tenant ID to which the clients belong.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the client cursor for pagination.
   * @param lastCreatedOn the creation timestamp cursor for pagination.
   * @return a {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllByTenantId(
      TenantId tenant, int limit, String lastClientId, ZonedDateTime lastCreatedOn);

  /**
   * Finds a client by its unique client ID and tenant ID.
   *
   * @param clientId the unique client ID.
   * @param tenant the tenant ID to which the client belongs.
   * @return an {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant);

  /**
   * Finds all clients that match the provided list of client IDs.
   *
   * @param clientIds a list of client IDs for which details are to be retrieved.
   * @return a list of {@link Client} objects matching the specified client IDs.
   */
  List<Client> findAllByClientIds(List<ClientId> clientIds);
}
