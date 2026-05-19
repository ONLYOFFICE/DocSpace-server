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
