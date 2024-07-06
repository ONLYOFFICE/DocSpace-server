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

package com.asc.registration.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.*;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;

/**
 * ClientApplicationService defines the contract for client-related operations in the application
 * layer. It includes methods for managing clients, such as creating, updating, deleting, and
 * retrieving client information, as well as handling client consents and activation states.
 */
public interface ClientApplicationService {

  /**
   * Retrieves detailed information about a specific tenant client. Accessible by admin users only.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A response containing the client's details.
   */
  ClientResponse getClient(@Valid TenantClientQuery query);

  /**
   * Retrieves basic information about a specific client. Fetches either only public clients with
   * the client ID or any client with both client ID and tenant ID.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A response containing the client's basic information.
   */
  ClientInfoResponse getClientInfo(@Valid ClientInfoQuery query);

  /**
   * Retrieves a paginated list of basic client information for a specific tenant. Fetches either
   * only public clients or any clients for the specified tenant ID.
   *
   * @param query The query containing the tenant ID, pagination parameters, and other filters.
   * @return A pageable response containing a list of client info responses.
   */
  PageableResponse<ClientInfoResponse> getClientsInfo(@Valid ClientInfoPaginationQuery query);

  /**
   * Retrieves basic information about a specific client. Fetches any existing client info
   *
   * @param clientId the clientId to fetch information for
   * @return A response containing the client's basic information.
   */
  ClientInfoResponse getClientInfo(@NotBlank String clientId);

  /**
   * Retrieves a paginated list of clients for a specific tenant. Accessible by admin users only.
   *
   * @param query The query containing pagination parameters and tenant ID.
   * @return A pageable response containing a list of client responses.
   */
  PageableResponse<ClientResponse> getClients(@Valid TenantClientsPaginationQuery query);

  /**
   * Retrieves a paginated list of consents for a specific principal name. Returns consents for
   * private tenant apps and all the public apps.
   *
   * @param query The query containing pagination parameters and principal name.
   * @return A pageable response containing a list of consent responses.
   */
  PageableResponse<ConsentResponse> getConsents(@Valid ConsentsPaginationQuery query);

  /**
   * Creates a new client for a specific tenant.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the details for creating a new client.
   * @return A response containing the created client's details.
   */
  ClientResponse createClient(@Valid Audit audit, @Valid CreateTenantClientCommand command);

  /**
   * Regenerates the secret key for a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID and client ID.
   * @return A response containing the new client secret.
   */
  ClientSecretResponse regenerateSecret(
      @Valid Audit audit, @Valid RegenerateTenantClientSecretCommand command);

  /**
   * Changes the activation state of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID, client ID, and the new activation state.
   */
  void changeActivation(@Valid Audit audit, @Valid ChangeTenantClientActivationCommand command);

  /**
   * Changes the visibility state of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID, client ID, and the new visibility state.
   */
  void changeVisibility(@Valid Audit audit, @Valid ChangeTenantClientVisibilityCommand command);

  /**
   * Updates the details of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the updated client details.
   * @return A response containing the updated client's details.
   */
  ClientResponse updateClient(@Valid Audit audit, @Valid UpdateTenantClientCommand command);

  /**
   * Deletes a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID and client ID.
   */
  void deleteClient(@Valid Audit audit, @Valid DeleteTenantClientCommand command);

  /**
   * Revokes the consent of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the client ID and principal name.
   */
  void revokeClientConsent(@Valid Audit audit, @Valid RevokeClientConsentCommand command);
}
