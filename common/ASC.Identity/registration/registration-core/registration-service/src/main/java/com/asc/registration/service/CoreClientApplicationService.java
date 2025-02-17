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

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.validation.annotation.Validated;

/**
 * Service class providing core client application functionalities. This service handles client
 * creation, update, deletion, and retrieval operations. It also manages client consent, visibility,
 * and activation states.
 */
@Service
@Validated
@RequiredArgsConstructor
public class CoreClientApplicationService implements ClientApplicationService {
  private final ClientCreateCommandHandler clientCreateCommandHandler;
  private final ClientUpdateCommandHandler clientUpdateCommandHandler;
  private final ClientQueryHandler clientQueryHandler;

  /**
   * Retrieves detailed client information for a specific tenant and client.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A {@link ClientResponse} containing detailed client information.
   */
  public ClientResponse getClient(TenantClientQuery query) {
    return clientQueryHandler.getClient(query);
  }

  /**
   * Retrieves detailed client information by client ID.
   *
   * @param clientId The unique identifier of the client.
   * @return A {@link ClientResponse} containing detailed client information.
   */
  public ClientResponse getClient(String clientId) {
    return clientQueryHandler.getClient(clientId);
  }

  /**
   * Retrieves basic client information for a specific tenant and client.
   *
   * @param query The query containing the client ID and optional tenant ID.
   * @return A {@link ClientInfoResponse} containing basic client information.
   */
  public ClientInfoResponse getClientInfo(ClientInfoQuery query) {
    return clientQueryHandler.getClientInfo(query);
  }

  /**
   * Retrieves basic client information by client ID.
   *
   * @param clientId The unique identifier of the client.
   * @return A {@link ClientInfoResponse} containing basic client information.
   */
  public ClientInfoResponse getClientInfo(String clientId) {
    return clientQueryHandler.getClientInfo(clientId);
  }

  /**
   * Retrieves a paginated list of basic client information for a specific tenant.
   *
   * @param query The query containing the tenant ID, pagination parameters, and filters.
   * @return A {@link PageableResponse} containing a list of {@link ClientInfoResponse}.
   */
  public PageableResponse<ClientInfoResponse> getClientsInfo(ClientInfoPaginationQuery query) {
    return clientQueryHandler.getClientsInfo(query);
  }

  /**
   * Retrieves a paginated list of clients for a specific tenant.
   *
   * @param query The query containing the tenant ID, pagination parameters, and filters.
   * @return A {@link PageableResponse} containing a list of {@link ClientResponse}.
   */
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    return clientQueryHandler.getClients(query);
  }

  public List<ClientResponse> getClients(List<ClientId> clientIds) {
    return clientQueryHandler.getClients(clientIds);
  }

  /**
   * Creates a new client for a tenant.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the details of the client to be created.
   * @return A {@link ClientResponse} containing the created client's information.
   */
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    return clientCreateCommandHandler.createClient(audit, command);
  }

  /**
   * Regenerates the secret key for a specific client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID and client ID.
   * @return A {@link ClientSecretResponse} containing the new client secret.
   */
  public ClientSecretResponse regenerateSecret(
      Audit audit, RegenerateTenantClientSecretCommand command) {
    return clientUpdateCommandHandler.regenerateSecret(audit, command);
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID, client ID, and new activation status.
   */
  public void changeActivation(Audit audit, ChangeTenantClientActivationCommand command) {
    clientUpdateCommandHandler.changeActivation(audit, command);
  }

  /**
   * Changes the visibility status of a client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID, client ID, and new visibility status.
   */
  public void changeVisibility(Audit audit, ChangeTenantClientVisibilityCommand command) {
    clientUpdateCommandHandler.changeVisibility(audit, command);
  }

  /**
   * Updates the information of an existing client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the updated client details.
   * @return A {@link ClientResponse} containing the updated client's information.
   */
  public ClientResponse updateClient(Audit audit, UpdateTenantClientCommand command) {
    return clientUpdateCommandHandler.updateClient(audit, command);
  }

  /**
   * Deletes an existing client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID and client ID of the client to be deleted.
   */
  public void deleteClient(Audit audit, DeleteTenantClientCommand command) {
    clientUpdateCommandHandler.deleteClient(audit, command);
  }
}
