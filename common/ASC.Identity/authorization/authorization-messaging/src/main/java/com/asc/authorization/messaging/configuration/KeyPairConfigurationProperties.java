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

package com.asc.authorization.messaging.configuration;

import java.time.Duration;
import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/** Configuration properties for key pair rotation settings. */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "spring.security.oauth2.registered-client")
public class KeyPairConfigurationProperties {
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

  /**
   * Returns the key rotation period.
   *
   * <p>Keys are rotated after this period (4x the access token TTL).
   *
   * @return the rotation period as a Duration
   */
  public Duration getRotationPeriod() {
    return Duration.ofMinutes(accessTokenMinutesTTL * 4L);
  }

  /**
   * Returns the key deprecation period.
   *
   * <p>Keys are deprecated (kept for validation) for this period after rotation.
   *
   * @return the deprecation period as a Duration
   */
  public Duration getDeprecationPeriod() {
    return Duration.ofMinutes(accessTokenMinutesTTL);
  }
}
