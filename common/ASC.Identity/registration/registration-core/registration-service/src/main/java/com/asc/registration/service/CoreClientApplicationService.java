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

package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.Role;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import jakarta.validation.ConstraintViolationException;
import jakarta.validation.Validator;
import java.util.List;
import lombok.RequiredArgsConstructor;

/**
 * Service class providing core client application functionalities. This service handles client
 * creation, update, deletion, and retrieval operations. It also manages client consent, visibility,
 * and activation states.
 */
@RequiredArgsConstructor
public class CoreClientApplicationService implements ClientApplicationService {
  private final Validator validator;
  private final ClientCacheService clientCacheService;

  private final AuthorizationMessagePublisher<TenantClientsRemovedEvent>
      tenantClientsMessagePublisher;
  private final AuthorizationMessagePublisher<UserClientsRemovedEvent> userClientsMessagePublisher;
  private final AuthorizationMessagePublisher<ClientCacheTenantRemoveEvent>
      clientCacheTenantRemoveMessagePublisher;

  private final ClientCreateCommandHandler clientCreateCommandHandler;
  private final ClientUpdateCommandHandler clientUpdateCommandHandler;
  private final ClientQueryHandler clientQueryHandler;

  /**
   * Validates an object using Jakarta Bean Validation and throws ConstraintViolationException if
   * validation fails.
   *
   * @param object the object to validate
   * @param <T> the type of the object
   * @throws ConstraintViolationException if validation fails
   */
  private <T> void validate(T object) {
    if (object == null) return;
    var violations = validator.validate(object);
    if (!violations.isEmpty()) throw new ConstraintViolationException(violations);
  }

  /**
   * Retrieves detailed client information for a specific tenant and client.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A {@link ClientResponse} containing detailed client information.
   */
  public ClientResponse getClient(Role role, TenantClientQuery query) {
    validate(query);
    return clientQueryHandler.getClient(role, query);
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
  public ClientInfoResponse getClientInfo(Role role, ClientInfoQuery query) {
    validate(query);
    return clientQueryHandler.getClientInfo(role, query);
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
  public PageableResponse<ClientInfoResponse> getClientsInfo(
      Role role, ClientInfoPaginationQuery query) {
    validate(query);
    return clientQueryHandler.getClientsInfo(role, query);
  }

  /**
   * Retrieves a paginated list of clients for a specific tenant.
   *
   * @param query The query containing the tenant ID, pagination parameters, and filters.
   * @return A {@link PageableResponse} containing a list of {@link ClientResponse}.
   */
  public PageableResponse<ClientResponse> getClients(
      Role role, TenantClientsPaginationQuery query) {
    validate(query);
    return clientQueryHandler.getClients(role, query);
  }

  /**
   * Retrieves a list of clients by their identifiers.
   *
   * @param clientIds The list of client identifiers to retrieve.
   * @return A list of {@link ClientResponse} containing the requested clients' information.
   */
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
    validate(command);
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
      Audit audit, Role role, RegenerateTenantClientSecretCommand command) {
    validate(command);
    return clientUpdateCommandHandler.regenerateSecret(audit, role, command);
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID, client ID, and new activation status.
   */
  public void changeActivation(
      Audit audit, Role role, ChangeTenantClientActivationCommand command) {
    validate(command);
    clientUpdateCommandHandler.changeActivation(audit, role, command);
  }

  /**
   * Changes the visibility status of a client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID, client ID, and new visibility status.
   */
  public void changeVisibility(
      Audit audit, Role role, ChangeTenantClientVisibilityCommand command) {
    validate(command);
    clientUpdateCommandHandler.changeVisibility(audit, role, command);
  }

  /**
   * Updates the information of an existing client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the updated client details.
   * @return A {@link ClientResponse} containing the updated client's information.
   */
  public ClientResponse updateClient(Audit audit, Role role, UpdateTenantClientCommand command) {
    validate(command);
    return clientUpdateCommandHandler.updateClient(audit, role, command);
  }

  /**
   * Deletes an existing client.
   *
   * @param audit The audit details of the operation, including the user performing it.
   * @param command The command containing the tenant ID and client ID of the client to be deleted.
   * @return the result of the delete operation, the number of rows affected.
   */
  public int deleteClient(Audit audit, Role role, DeleteTenantClientCommand command) {
    validate(command);
    return clientUpdateCommandHandler.deleteClient(audit, role, command);
  }

  /**
   * Deletes all clients created by a specific user within a tenant.
   *
   * @param command The command containing the tenant ID and user ID.
   * @return The number of clients deleted.
   */
  public int deleteUserClients(DeleteUserClientsCommand command) {
    validate(command);
    userClientsMessagePublisher.publish(
        UserClientsRemovedEvent.builder().userId(command.getUserId()).build());
    return clientUpdateCommandHandler.deleteUserClients(command);
  }

  /**
   * Deletes all clients associated with a specific tenant.
   *
   * @param command The command containing the tenant ID.
   * @return The number of clients deleted.
   */
  public int deleteTenantClients(DeleteTenantClientsCommand command) {
    validate(command);

    clientCacheService.evictAllByTenantId(new TenantId(command.getTenantId()));

    clientCacheTenantRemoveMessagePublisher.publish(
        ClientCacheTenantRemoveEvent.builder().tenantId(command.getTenantId()).build());
    tenantClientsMessagePublisher.publish(
        TenantClientsRemovedEvent.builder().tenantId(command.getTenantId()).build());

    return clientUpdateCommandHandler.deleteTenantClients(command.getTenantId());
  }
}
