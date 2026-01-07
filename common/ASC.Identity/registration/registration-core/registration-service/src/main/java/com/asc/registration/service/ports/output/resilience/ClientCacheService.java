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

package com.asc.registration.service.ports.output.resilience;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.Client;
import java.util.Optional;

/**
 * Service interface for managing client cache operations.
 *
 * <p>This service provides methods to store, retrieve, and evict client entities from the cache. It
 * is designed to work with domain events to maintain cache consistency with the database state.
 */
public interface ClientCacheService {

  /**
   * Stores a client entity in the cache.
   *
   * <p>This method is typically called when a client is created or updated to keep the cache
   * synchronized with the database.
   *
   * @param client The client entity to store in the cache.
   */
  void put(Client client);

  /**
   * Retrieves a client from the cache by its client ID and tenant ID.
   *
   * @param clientId The unique identifier of the client.
   * @param tenantId The tenant ID to which the client belongs.
   * @return An {@link Optional} containing the client if found in the cache, or empty otherwise.
   */
  Optional<Client> get(ClientId clientId, TenantId tenantId);

  /**
   * Retrieves a client from the cache by its client ID only, searching across all tenants.
   *
   * <p>This method scans the cache for a client with the given ID across all tenant partitions. Use
   * this when tenant context is unavailable but cache lookup is still desired.
   *
   * @param clientId The unique identifier of the client.
   * @return An {@link Optional} containing the client if found in the cache, or empty otherwise.
   */
  Optional<Client> getAnyTenant(ClientId clientId);

  /**
   * Evicts a client from the cache by its client ID.
   *
   * <p>This method is typically called when a client is deleted to remove it from the cache.
   *
   * @param clientId The unique identifier of the client to evict.
   */
  void evict(ClientId clientId, TenantId tenantId);

  /**
   * Evicts all clients from the cache that belong to a specific tenant.
   *
   * <p>This method is typically called during tenant cleanup operations.
   *
   * @param tenantId The tenant ID for which all clients should be evicted.
   */
  void evictAllByTenantId(TenantId tenantId);

  /**
   * Clears the entire client cache.
   *
   * <p>This method should be used with caution as it removes all cached clients.
   */
  void clear();
}
