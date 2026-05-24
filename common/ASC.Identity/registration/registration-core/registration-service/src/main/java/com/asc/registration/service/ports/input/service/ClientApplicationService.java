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

package com.asc.registration.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.Role;
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
 * Defines the contract for managing client entities within the application layer.
 *
 * <p>This interface provides methods for creating, updating, retrieving, and deleting clients, as
 * well as managing client-specific attributes such as activation status, visibility, and security
 * (e.g., secret regeneration). The behavior of several methods is determined by the role of the
 * user, ensuring that authorization is enforced appropriately.
 */
public interface ClientApplicationService {

  /**
   * Retrieves detailed information for a tenant client based on the provided query.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can retrieve details for any client within the tenant.
   *   <li>Regular users can only retrieve details for clients they own.
   * </ul>
   *
   * @param role the role of the user performing the operation.
   * @param query a {@link TenantClientQuery} containing the tenant and client identifiers.
   * @return a {@link ClientResponse} containing the detailed information of the specified client.
   */
  ClientResponse getClient(@NotNull Role role, @Valid TenantClientQuery query);

  /**
   * Retrieves detailed information for a client identified by its client ID.
   *
   * @param clientId the unique identifier of the client.
   * @return a {@link ClientResponse} containing the detailed information of the specified client.
   */
  ClientResponse getClient(@NotBlank String clientId);

  /**
   * Retrieves basic client information based on the provided query.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can access basic information for any client across tenants.
   *   <li>Regular users can access basic information only for clients they own.
   * </ul>
   *
   * @param role the role of the user performing the request.
   * @param query a {@link ClientInfoQuery} containing the client identifier and, optionally, the
   *     tenant identifier.
   * @return a {@link ClientInfoResponse} containing the basic information of the specified client.
   */
  ClientInfoResponse getClientInfo(@NotNull Role role, @Valid ClientInfoQuery query);

  /**
   * Retrieves basic client information for the specified client ID.
   *
   * <p>This is a public method that does not require tenant verification.
   *
   * @param clientId the unique identifier of the client.
   * @return a {@link ClientInfoResponse} containing the basic information of the client.
   */
  ClientInfoResponse getClientInfo(@NotBlank String clientId);

  /**
   * Retrieves a paginated list of basic client information for a specific tenant.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can retrieve all clients associated with the tenant.
   *   <li>Regular users can retrieve only the clients accessible to them.
   * </ul>
   *
   * @param role the role of the user making the request.
   * @param query a {@link ClientInfoPaginationQuery} containing pagination parameters, tenant
   *     identifier, and optional filters.
   * @return a {@link PageableResponse} containing a paginated list of {@link ClientInfoResponse}
   *     objects.
   */
  PageableResponse<ClientInfoResponse> getClientsInfo(
      @NotNull Role role, @Valid ClientInfoPaginationQuery query);

  /**
   * Retrieves a paginated list of clients for a specific tenant.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can retrieve all clients for the tenant.
   *   <li>Regular users can retrieve only the clients accessible to them.
   * </ul>
   *
   * @param role the role of the user performing the operation.
   * @param query a {@link TenantClientsPaginationQuery} containing pagination parameters and the
   *     tenant identifier.
   * @return a {@link PageableResponse} containing a paginated list of {@link ClientResponse}
   *     objects.
   */
  PageableResponse<ClientResponse> getClients(
      @NotNull Role role, @Valid TenantClientsPaginationQuery query);

  /**
   * Retrieves detailed information for a list of clients identified by their client IDs.
   *
   * @param clientIds a list of {@link ClientId} objects representing the unique identifiers of the
   *     clients.
   * @return a list of {@link ClientResponse} objects containing detailed information for the
   *     specified clients.
   */
  List<ClientResponse> getClients(@Valid @NotNull List<ClientId> clientIds);

  /**
   * Creates a new client for a specified tenant.
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     operation.
   * @param command a {@link CreateTenantClientCommand} containing the details required to create
   *     the client.
   * @return a {@link ClientResponse} containing the details of the newly created client.
   */
  ClientResponse createClient(@Valid Audit audit, @Valid CreateTenantClientCommand command);

  /**
   * Regenerates the secret key for a specified client.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can regenerate the secret for any client within the tenant.
   *   <li>Regular users can only regenerate the secret for clients they own.
   * </ul>
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     operation.
   * @param role the role of the user requesting the secret regeneration.
   * @param command a {@link RegenerateTenantClientSecretCommand} containing the tenant and client
   *     identifiers.
   * @return a {@link ClientSecretResponse} containing the newly generated client secret.
   */
  ClientSecretResponse regenerateSecret(
      @Valid Audit audit, @NotNull Role role, @Valid RegenerateTenantClientSecretCommand command);

  /**
   * Updates the activation state of a specified client.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can change the activation state for any client within the tenant.
   *   <li>Regular users can change the activation state only for the clients they own.
   * </ul>
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     operation.
   * @param role the role of the user performing the update.
   * @param command a {@link ChangeTenantClientActivationCommand} containing the tenant and client
   *     identifiers, along with the new activation state.
   */
  void changeActivation(
      @Valid Audit audit, @NotNull Role role, @Valid ChangeTenantClientActivationCommand command);

  /**
   * Updates the visibility state of a specified client.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can change the visibility state for any client within the tenant.
   *   <li>Regular users can change the visibility state only for the clients they own.
   * </ul>
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     operation.
   * @param role the role of the user performing the update.
   * @param command a {@link ChangeTenantClientVisibilityCommand} containing the tenant and client
   *     identifiers, along with the new visibility state.
   */
  void changeVisibility(
      @Valid Audit audit, @NotNull Role role, @Valid ChangeTenantClientVisibilityCommand command);

  /**
   * Updates the details of an existing client.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can update any client within the tenant.
   *   <li>Regular users can update only the clients they own.
   * </ul>
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     update.
   * @param role the role of the user performing the update.
   * @param command a {@link UpdateTenantClientCommand} containing the updated client details.
   * @return a {@link ClientResponse} containing the updated details of the client.
   */
  ClientResponse updateClient(
      @Valid Audit audit, @NotNull Role role, @Valid UpdateTenantClientCommand command);

  /**
   * Deletes a specified client.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>Administrators can delete any client within the tenant.
   *   <li>Regular users can delete only the clients they own.
   * </ul>
   *
   * @param audit an {@link Audit} object capturing the audit details of the user performing the
   *     deletion.
   * @param role the role of the user performing the deletion.
   * @param command a {@link DeleteTenantClientCommand} containing the tenant and client identifiers
   *     for deletion.
   * @return the result of the delete operation, the number of rows affected.
   */
  int deleteClient(
      @Valid Audit audit, @NotNull Role role, @Valid DeleteTenantClientCommand command);

  /**
   * Deletes all clients owned by a specific user.
   *
   * <p>This operation is typically used during user deprovisioning to ensure proper cleanup of
   * user-related resources. The deletion is performed based on the user identifier provided in the
   * command.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>This operation is typically restricted to administrative or system-level processes.
   * </ul>
   *
   * @param command a {@link DeleteUserClientsCommand} containing the user and tenant identifiers
   *     for clients to be deleted.
   * @return the number of clients successfully deleted.
   */
  int deleteUserClients(@Valid DeleteUserClientsCommand command);

  /**
   * Deletes all clients associated with a specific tenant.
   *
   * <p>This operation is typically used during tenant deprovisioning to ensure proper cleanup of
   * tenant-related resources. The deletion is performed based on the tenant identifier provided in
   * the command.
   *
   * <p>Access control:
   *
   * <ul>
   *   <li>This operation is typically restricted to administrative or system-level processes.
   * </ul>
   *
   * @param command a {@link DeleteTenantClientsCommand} containing the tenant identifier for
   *     clients to be deleted.
   * @return the number of clients successfully deleted.
   */
  int deleteTenantClients(@Valid DeleteTenantClientsCommand command);
}
