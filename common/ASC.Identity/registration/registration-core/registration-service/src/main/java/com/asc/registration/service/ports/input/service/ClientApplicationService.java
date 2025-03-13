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
  ClientResponse getClient(@Valid String clientId);

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
}
