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

  /**
   * Deletes all clients created by a specific user within a tenant from the repository.
   *
   * <p>This method is typically used during user cleanup or deprovisioning operations to remove all
   * client entities associated with a particular user within a tenant.
   *
   * @param tenantId The tenant ID to filter clients by.
   * @param userId The ID of the user who created the clients to be deleted.
   * @return The number of clients deleted.
   */
  int deleteAllByTenantIdAndCreatedBy(TenantId tenantId, UserId userId);

  /**
   * Deletes all clients belonging to a specific tenant from the repository.
   *
   * <p>This method is typically used during tenant cleanup or deprovisioning operations to remove
   * all client entities associated with a particular tenant.
   *
   * @param tenantId The tenant ID of the clients to be deleted.
   * @return The number of clients deleted.
   */
  int deleteAllByTenantId(TenantId tenantId);
}
