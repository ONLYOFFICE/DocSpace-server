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
package com.asc.authorization.application.configuration;

import com.github.benmanes.caffeine.cache.Caffeine;
import java.util.concurrent.TimeUnit;
import org.springframework.cache.caffeine.CaffeineCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for setting up cache management using Caffeine cache.
 *
 * <p>This configuration provides a {@link CaffeineCacheManager} bean with pre-configured cache
 * settings for application-wide caching needs.
 *
 * @see CaffeineCacheManager
 * @see Caffeine
 */
@Configuration
public class CacheManagerConfiguration {
  /**
   * Builds and configures a Caffeine cache instance with specific eviction policies.
   *
   * @return a configured {@link Caffeine} builder instance
   */
  Caffeine<Object, Object> caffeineCacheBuilder() {
    return Caffeine.newBuilder().expireAfterWrite(5, TimeUnit.MINUTES).maximumSize(50);
  }

  /**
   * Creates and configures a {@link CaffeineCacheManager} bean for Spring's cache abstraction.
   *
   * <p>The cache manager is initialized with a cache named "key_pair" and applies the configuration
   * from {@link #caffeineCacheBuilder()}.
   *
   * @return a configured {@link CaffeineCacheManager} instance with the "key_pair" cache
   * @see #caffeineCacheBuilder()
   */
  @Bean
  public CaffeineCacheManager cacheManager() {
    CaffeineCacheManager cacheManager = new CaffeineCacheManager("key_pair");
    cacheManager.setCaffeine(caffeineCacheBuilder());
    return cacheManager;
  }
}
