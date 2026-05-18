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

package com.asc.registration.service;

import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.ports.output.resilience.ScopeCacheService;
import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Set;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

/**
 * CoreScopeApplicationService implements the {@link ScopeApplicationService} interface, providing
 * core business logic for managing scopes.
 */
@Slf4j
@RequiredArgsConstructor
public class CoreScopeApplicationService implements ScopeApplicationService {
  private final ScopeCacheService scopeCacheService;
  private final ScopeQueryHandler queryHandler;

  /**
   * Retrieves all available scopes.
   *
   * @return a set of {@link ScopeResponse} representing all scopes.
   */
  public Set<ScopeResponse> getScopes() {
    log.debug("Retrieving all scopes");

    var cached = scopeCacheService.getAll().orElse(null);
    if (cached != null) {
      log.debug("Returning {} scopes from cache", cached.size());
      return cached;
    }

    var scopes = queryHandler.getScopes();
    scopeCacheService.putAll(scopes);
    return scopes;
  }

  /**
   * Retrieves a specific scope by its name.
   *
   * @param name the name of the scope to retrieve.
   * @return a {@link ScopeResponse} representing the requested scope.
   */
  public ScopeResponse getScope(String name) {
    log.debug("Retrieving scope by name: {}", name);

    var cached = scopeCacheService.get(name).orElse(null);
    if (cached != null) {
      log.debug("Returning scope {} from cache", name);
      return cached;
    }

    getScopes();

    return scopeCacheService.get(name).orElseGet(() -> queryHandler.getScope(name));
  }
}
