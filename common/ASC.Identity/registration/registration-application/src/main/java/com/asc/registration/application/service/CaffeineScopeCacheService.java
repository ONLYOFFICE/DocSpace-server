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

package com.asc.registration.application.service;

import com.asc.registration.service.ports.output.resilience.ScopeCacheService;
import com.asc.registration.service.transfer.response.ScopeResponse;
import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import java.util.Optional;
import java.util.Set;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

/**
 * Implementation of {@link ScopeCacheService} using Caffeine as an in-memory cache.
 *
 * <p>This service provides fast, local caching for scope data. Scopes are static configuration data
 * and are permanently cached in memory (no expiration). The cache is populated on application
 * startup or on first access.
 *
 * @see ScopeCacheService
 * @see ScopeResponse
 */
@Slf4j
@Service
public class CaffeineScopeCacheService implements ScopeCacheService {
  private static final String ALL_SCOPES_KEY = "__ALL_SCOPES__";
  private static final int CACHE_MAX_SIZE = 1000;

  private final Cache<String, ScopeResponse> scopeCache;
  private final Cache<String, Set<ScopeResponse>> allScopesCache;

  /**
   * Constructs a new InMemoryScopeCacheService with Caffeine cache.
   *
   * <p>The cache is configured for permanent storage with:
   *
   * <ul>
   *   <li>No expiration - scopes are static configuration data
   *   <li>Maximum 1000 entries - scopes are typically few in number
   *   <li>Strong references - ensures scopes remain in memory permanently
   * </ul>
   */
  public CaffeineScopeCacheService() {
    this.scopeCache = Caffeine.newBuilder().maximumSize(CACHE_MAX_SIZE).build();
    this.allScopesCache = Caffeine.newBuilder().maximumSize(1).build();

    log.info("Initialized InMemoryScopeCacheService with permanent Caffeine cache");
  }

  /**
   * Stores a scope in the cache.
   *
   * @param scope The scope response to cache. Must not be null and must have a non-null name.
   */
  @Override
  public void put(ScopeResponse scope) {
    if (scope == null || scope.getName() == null) {
      log.warn("Attempted to cache null scope or scope with null name");
      return;
    }

    try {
      scopeCache.put(scope.getName(), scope);
      log.debug("Cached scope with name: {}", scope.getName());
    } catch (Exception e) {
      log.warn("Failed to cache scope: {}", scope.getName(), e);
    }
  }

  /**
   * Stores all scopes in the cache.
   *
   * <p>This method caches the complete set of scopes for efficient retrieval of all scopes at once.
   * Individual scopes are also cached for name-based lookups.
   *
   * @param scopes The set of scope responses to cache.
   */
  @Override
  public void putAll(Set<ScopeResponse> scopes) {
    if (scopes == null || scopes.isEmpty()) {
      log.debug("Attempted to cache null or empty scopes set");
      return;
    }

    try {
      allScopesCache.put(ALL_SCOPES_KEY, scopes);
      scopes.forEach(this::put);
      log.debug("Cached {} scopes", scopes.size());
    } catch (Exception e) {
      log.warn("Failed to cache all scopes", e);
    }
  }

  /**
   * Retrieves a scope from the cache by its name.
   *
   * @param name The name of the scope to retrieve. If null, returns empty Optional.
   * @return An Optional containing the scope if found, or empty if not found or name is null.
   */
  @Override
  public Optional<ScopeResponse> get(String name) {
    if (name == null) return Optional.empty();

    try {
      var cached = scopeCache.getIfPresent(name);
      if (cached != null) {
        log.debug("Cache hit for scope: {}", name);
        return Optional.of(cached);
      }
    } catch (Exception e) {
      log.warn("Failed to retrieve scope from cache: {}", name, e);
    }

    log.debug("Cache miss for scope: {}", name);
    return Optional.empty();
  }

  /**
   * Retrieves all scopes from the cache.
   *
   * @return An Optional containing all scopes if cached, or empty if not cached.
   */
  @Override
  public Optional<Set<ScopeResponse>> getAll() {
    try {
      var cached = allScopesCache.getIfPresent(ALL_SCOPES_KEY);
      if (cached != null) {
        log.debug("Cache hit for all scopes");
        return Optional.of(cached);
      }
    } catch (Exception e) {
      log.warn("Failed to retrieve all scopes from cache", e);
    }

    log.debug("Cache miss for all scopes");
    return Optional.empty();
  }

  /**
   * Removes a scope from the cache by its name.
   *
   * @param name The name of the scope to evict. If null, no operation is performed.
   */
  @Override
  public void evict(String name) {
    if (name == null) {
      log.warn("Attempted to evict scope with null name");
      return;
    }

    try {
      scopeCache.invalidate(name);
      allScopesCache.invalidate(ALL_SCOPES_KEY);
      log.debug("Evicted scope from cache: {}", name);
    } catch (Exception e) {
      log.error("Failed to evict scope from cache: {}", name, e);
    }
  }

  /**
   * Clears the entire scope cache.
   *
   * <p>This method removes all cached scopes, both individual entries and the complete set.
   */
  @Override
  public void clear() {
    try {
      scopeCache.invalidateAll();
      allScopesCache.invalidateAll();
      log.debug("Cleared entire scope cache");
    } catch (Exception e) {
      log.error("Failed to clear scope cache", e);
    }
  }
}
