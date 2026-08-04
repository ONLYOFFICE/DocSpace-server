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

package com.asc.authorization.data.client.cache;

import java.util.Optional;

/** Service interface for caching registered clients resolved from the registration service. */
public interface RegisteredClientCacheService {

  /**
   * Stores a client in the cache.
   *
   * @param client the client to cache.
   */
  void put(CachedRegisteredClient client);

  /**
   * Retrieves a client from the cache by its client ID, regardless of tenant.
   *
   * @param clientId the client ID to look up.
   * @return an {@link Optional} containing the cached client, or empty if not present.
   */
  Optional<CachedRegisteredClient> get(String clientId);

  /**
   * Evicts a single client from the cache.
   *
   * @param clientId the client ID to evict.
   * @param tenantId the tenant ID the client belongs to.
   */
  void evict(String clientId, long tenantId);

  /**
   * Evicts every client belonging to a tenant from the cache.
   *
   * @param tenantId the tenant ID whose clients should be evicted.
   */
  void evictAllByTenantId(long tenantId);

  /** Clears the entire registered client cache. */
  void clear();
}
