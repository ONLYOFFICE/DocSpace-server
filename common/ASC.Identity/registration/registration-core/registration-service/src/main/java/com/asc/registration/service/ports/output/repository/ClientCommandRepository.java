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
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientEvent;

/**
 * ClientCommandRepository defines the contract for client-related operations that modify the state
 * of clients. This repository handles saving clients, regenerating client secrets, changing
 * visibility, activation states, and deleting clients.
 */
public interface ClientCommandRepository {

  /**
   * Saves a client entity to the repository.
   *
   * @param client The client entity to be saved.
   * @return The saved client entity.
   */
  Client saveClient(ClientEvent event, Client client);

  /**
   * Updates an existing client entity in the repository. This method replaces the current state of
   * the client with the new values provided in the updated client entity.
   *
   * <p>The client is identified by its ID, and only the specified fields will be updated based on
   * the provided {@link Client} object.
   *
   * @param client The client entity containing the updated values.
   * @return The updated client entity after it has been persisted.
   */
  Client updateClient(ClientEvent event, Client client);

  /**
   * Regenerates the secret key for a specific client identified by tenant ID and client ID.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID for which the secret is to be regenerated.
   * @return The new client secret.
   */
  String regenerateClientSecretByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId);

  /**
   * Changes the visibility of a specific client identified by tenant ID and client ID.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID for which the visibility is to be changed.
   * @param visible The new visibility state (true for public, false for private).
   */
  void changeVisibilityByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId, boolean visible);

  /**
   * Changes the activation state of a specific client identified by tenant ID and client ID.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID for which the activation state is to be changed.
   * @param enabled The new activation state (true for enabled, false for disabled).
   */
  void changeActivationByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId, boolean enabled);

  /**
   * Deletes a specific client identified by tenant ID and client ID from the repository.
   *
   * @param tenantId The tenant ID to which the client belongs.
   * @param clientId The client ID of the client to be deleted.
   * @return The number of clients deleted (typically 0 or 1).
   */
  int deleteByTenantIdAndClientId(ClientEvent event, TenantId tenantId, ClientId clientId);
}
