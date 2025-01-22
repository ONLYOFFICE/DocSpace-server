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

package com.asc.registration.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.util.List;

/**
 * Interface defining the contract for managing clients within the application layer.
 *
 * <p>Provides methods for creating, updating, retrieving, and deleting clients. It also supports
 * managing client-related attributes, such as consents, activation state, and visibility settings.
 */
public interface ClientApplicationService {

  /**
   * Retrieves detailed information about a specific tenant client. Accessible only by admin users.
   *
   * @param query the query containing the tenant ID and client ID.
   * @return a {@link ClientResponse} containing the client's details.
   */
  ClientResponse getClient(@Valid TenantClientQuery query);

  /**
   * Retrieves detailed information about a specific client using its client ID.
   *
   * @param clientId the unique identifier of the client.
   * @return a {@link ClientResponse} containing the client's details.
   */
  ClientResponse getClient(@Valid String clientId);

  /**
   * Retrieves basic information about a specific client. Allows fetching either public clients
   * using only the client ID, or any client using both the client ID and tenant ID.
   *
   * @param query the query containing the client ID and optionally the tenant ID.
   * @return a {@link ClientInfoResponse} containing the client's basic information.
   */
  ClientInfoResponse getClientInfo(@Valid ClientInfoQuery query);

  /**
   * Retrieves basic information about a specific client using only its client ID.
   *
   * @param clientId the unique identifier of the client.
   * @return a {@link ClientInfoResponse} containing the client's basic information.
   */
  ClientInfoResponse getClientInfo(@NotBlank String clientId);

  /**
   * Retrieves a paginated list of basic information about clients for a specific tenant. Fetches
   * either public clients or all clients associated with the specified tenant ID.
   *
   * @param query the query containing pagination parameters, tenant ID, and other filters.
   * @return a {@link PageableResponse} containing a list of {@link ClientInfoResponse}.
   */
  PageableResponse<ClientInfoResponse> getClientsInfo(@Valid ClientInfoPaginationQuery query);

  /**
   * Retrieves a paginated list of clients for a specific tenant. Accessible only by admin users.
   *
   * @param query the query containing pagination parameters and tenant ID.
   * @return a {@link PageableResponse} containing a list of {@link ClientResponse}.
   */
  PageableResponse<ClientResponse> getClients(@Valid TenantClientsPaginationQuery query);

  /**
   * Retrieves a list of clients for the given set of client IDs.
   *
   * @param clientIds a list of client IDs for which details are to be retrieved.
   * @return a list of {@link ClientResponse} containing details of the specified clients.
   */
  List<ClientResponse> getClients(@Valid @NotNull List<ClientId> clientIds);

  /**
   * Creates a new client for a specific tenant.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the details required to create a new client.
   * @return a {@link ClientResponse} containing the created client's details.
   */
  ClientResponse createClient(@Valid Audit audit, @Valid CreateTenantClientCommand command);

  /**
   * Regenerates the secret key for a specific client.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the tenant ID and client ID.
   * @return a {@link ClientSecretResponse} containing the new client secret.
   */
  ClientSecretResponse regenerateSecret(
      @Valid Audit audit, @Valid RegenerateTenantClientSecretCommand command);

  /**
   * Updates the activation state of a specific client.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the tenant ID, client ID, and the new activation state.
   */
  void changeActivation(@Valid Audit audit, @Valid ChangeTenantClientActivationCommand command);

  /**
   * Updates the visibility state of a specific client.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the tenant ID, client ID, and the new visibility state.
   */
  void changeVisibility(@Valid Audit audit, @Valid ChangeTenantClientVisibilityCommand command);

  /**
   * Updates the details of a specific client.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the updated client details.
   * @return a {@link ClientResponse} containing the updated client's details.
   */
  ClientResponse updateClient(@Valid Audit audit, @Valid UpdateTenantClientCommand command);

  /**
   * Deletes a specific client.
   *
   * @param audit the audit details of the user performing the operation.
   * @param command the command containing the tenant ID and client ID.
   */
  void deleteClient(@Valid Audit audit, @Valid DeleteTenantClientCommand command);
}
