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

package com.asc.registration.application.service;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.utilities.cache.CacheTicket;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import java.util.Optional;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

/**
 * No-op implementation of {@link ClientCacheService} used when the Redis client cache is disabled
 * ({@code client.cache.redis.enabled} is not {@code true}).
 *
 * <p>Every lookup misses, so clients are always read from the store. An in-process cache alone
 * would be unsafe here: without Redis there is no shared namespace and no eviction broadcast, so
 * other instances would keep serving clients that were updated or removed. The minified profile,
 * which runs as a single instance, keeps its own in-memory implementation instead.
 */
@Service
@Profile("!minified")
@ConditionalOnProperty(
    prefix = "client.cache.redis",
    name = "enabled",
    havingValue = "false",
    matchIfMissing = true)
public class NoOpClientCacheService implements ClientCacheService {
  @Override
  public void put(Client client) {}

  @Override
  public CacheTicket ticket(ClientId clientId) {
    return new CacheTicket(0, null);
  }

  @Override
  public void putUnlessInvalidated(Client client, CacheTicket ticket) {}

  @Override
  public Optional<Client> get(ClientId clientId, TenantId tenantId) {
    return Optional.empty();
  }

  @Override
  public Optional<Client> getAnyTenant(ClientId clientId) {
    return Optional.empty();
  }

  @Override
  public void evict(ClientId clientId, TenantId tenantId) {}

  @Override
  public void evictAllByTenantId(TenantId tenantId) {}

  @Override
  public void clear() {}
}
