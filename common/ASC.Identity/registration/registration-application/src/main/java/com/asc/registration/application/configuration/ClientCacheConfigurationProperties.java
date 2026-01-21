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
