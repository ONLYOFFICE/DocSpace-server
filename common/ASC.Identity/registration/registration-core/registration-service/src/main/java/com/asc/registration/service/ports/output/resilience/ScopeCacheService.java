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

import com.asc.registration.service.transfer.response.ScopeResponse;
import java.util.Optional;
import java.util.Set;

/**
 * Service interface for managing scope cache operations.
 *
 * <p>This service provides methods to store, retrieve, and evict scope responses from an in-memory
 * cache to improve performance for frequently accessed scope data.
 */
public interface ScopeCacheService {

  /**
   * Stores a scope in the cache.
   *
   * @param scope The scope response to cache. Must not be null and must have a non-null name.
   */
  void put(ScopeResponse scope);

  /**
   * Stores all scopes in the cache.
   *
   * @param scopes The set of scope responses to cache.
   */
  void putAll(Set<ScopeResponse> scopes);

  /**
   * Retrieves a scope from the cache by its name.
   *
   * @param name The name of the scope to retrieve. If null, returns empty Optional.
   * @return An Optional containing the scope if found, or empty if not found or name is null.
   */
  Optional<ScopeResponse> get(String name);

  /**
   * Retrieves all scopes from the cache.
   *
   * @return An Optional containing all scopes if cached, or empty if not cached.
   */
  Optional<Set<ScopeResponse>> getAll();

  /**
   * Removes a scope from the cache by its name.
   *
   * @param name The name of the scope to evict. If null, no operation is performed.
   */
  void evict(String name);

  /** Clears the entire scope cache. */
  void clear();
}
