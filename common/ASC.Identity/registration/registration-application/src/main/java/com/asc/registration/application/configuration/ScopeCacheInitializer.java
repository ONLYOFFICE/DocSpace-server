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

package com.asc.registration.application.configuration;

import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.event.EventListener;

/**
 * ScopeCacheInitializer pre-loads all scopes into the in-memory cache on application startup.
 *
 * <p>Since scopes are static configuration data that rarely changes, they are loaded once at
 * startup and cached permanently in memory for fast access throughout the application lifecycle.
 */
@Slf4j
@Configuration
@RequiredArgsConstructor
public class ScopeCacheInitializer {
  private final ScopeApplicationService scopeApplicationService;

  /**
   * Initializes the scope cache when the application is ready.
   *
   * <p>This method is triggered by the {@link ApplicationReadyEvent}, ensuring that all scopes are
   * loaded into cache before the application starts serving requests.
   */
  @EventListener(ApplicationReadyEvent.class)
  public void initializeScopeCache() {
    try {
      var scopes = scopeApplicationService.getScopes();
      log.info("Successfully pre-loaded {} scopes into cache", scopes.size());
    } catch (Exception e) {
      log.error("Failed to initialize scope cache on startup", e);
    }
  }
}
