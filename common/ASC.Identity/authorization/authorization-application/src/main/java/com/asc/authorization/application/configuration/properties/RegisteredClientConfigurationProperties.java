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

package com.asc.authorization.application.configuration.properties;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration properties for OAuth2 registered client settings.
 *
 * <p>This class defines configurable properties for managing token lifetimes in an OAuth2
 * authorization server. The properties are loaded from the application's configuration file and can
 * be customized using the prefix {@code spring.security.oauth2.registered-client}.
 */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "spring.security.oauth2.registered-client")
public class RegisteredClientConfigurationProperties {
  /**
   * Time-to-live (TTL) for access tokens, in minutes.
   *
   * <p>This property determines how long an access token remains valid before it expires. The
   * default value is 60 minutes.
   */
  private int accessTokenMinutesTTL = 60;

  /**
   * Time-to-live (TTL) for refresh tokens, in days.
   *
   * <p>This property defines the lifespan of a refresh token before it becomes invalid. The default
   * value is 30 days.
   */
  private int refreshTokenDaysTTL = 30;

  /**
   * Time-to-live (TTL) for authorization codes, in minutes.
   *
   * <p>This property specifies the validity period of an authorization code. The default value is 1
   * minute.
   */
  private int authorizationCodeMinutesTTL = 1;
}
