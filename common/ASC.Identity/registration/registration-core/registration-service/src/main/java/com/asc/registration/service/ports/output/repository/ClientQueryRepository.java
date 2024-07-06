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

package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Optional;

/**
 * ClientQueryRepository defines the contract for client-related query operations. This repository
 * handles retrieving clients based on various query parameters.
 */
public interface ClientQueryRepository {

  /**
   * Finds a client by its unique client ID and visibility status.
   *
   * @param clientId The unique client ID.
   * @param visibility The visibility status of the client.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByIdAndVisibility(ClientId clientId, ClientVisibility visibility);

  /**
   * Finds a client by its unique client ID.
   *
   * @param clientId The unique client ID.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findById(ClientId clientId);

  /**
   * Finds all public and private clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant The tenant ID to which the clients belong.
   * @param page The page number to retrieve.
   * @param limit The number of clients per page.
   * @return A {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllPublicAndPrivateByTenantId(TenantId tenant, int page, int limit);

  /**
   * Finds all clients belonging to a specific tenant, with pagination support.
   *
   * @param tenant The tenant ID to which the clients belong.
   * @param page The page number to retrieve.
   * @param limit The number of clients per page.
   * @return A {@link PageableResponse} containing the clients for the specified tenant.
   */
  PageableResponse<Client> findAllByTenantId(TenantId tenant, int page, int limit);

  /**
   * Finds a client by its unique client ID and tenant ID.
   *
   * @param clientId The unique client ID.
   * @param tenant The tenant ID to which the client belongs.
   * @return An {@link Optional} containing the client if found, or an empty {@link Optional} if not
   *     found.
   */
  Optional<Client> findByClientIdAndTenantId(ClientId clientId, TenantId tenant);
}
