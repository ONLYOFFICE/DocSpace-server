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

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * Configuration properties for client cache Redis connection. Provides settings for Redis
 * connection that can be customized via application properties using the `client.cache.redis`
 * prefix.
 */
@Data
@ConfigurationProperties(prefix = "client.cache.redis")
public class ClientCacheConfigurationProperties {
  /** The host of the Redis server. Defaults to localhost. */
  private String host = "localhost";

  /** The port of the Redis server. Defaults to 6379. */
  private int port = 6379;

  /** The Redis database index to use. Defaults to 0. */
  private int database = 0;

  /** The username for authenticating with the Redis server. Optional. */
  private String username;

  /** The password for authenticating with the Redis server. Optional. */
  private String password;

  /** Indicates whether SSL is enabled for the Redis connection. Defaults to false. */
  private boolean ssl = false;
}
