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
