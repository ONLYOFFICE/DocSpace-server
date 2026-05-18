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

package com.asc.common.service.transfer.message;

import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Represents an event indicating that a user login has been registered.
 *
 * <p>This event is used to notify systems about user login activities, allowing audit tracking and
 * security monitoring to be triggered.
 */
@Builder
@Getter
@Setter
@ToString
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class LoginRegisteredEvent {

  /** The login identifier or username associated with this event. */
  @Size(max = 200)
  private String login;

  /** Flag indicating whether the login session is currently active. */
  private boolean active;

  /** The IP address from which the login was performed. */
  @Size(max = 50)
  private String ip;

  /** Information about the browser used for the login. */
  @Size(max = 200)
  private String browser;

  /** Information about the platform (operating system) used for the login. */
  @Size(max = 200)
  private String platform;

  /** The timestamp when the login event occurred. */
  @NotNull private ZonedDateTime date;

  /** The tenant identifier associated with this login event. */
  private long tenantId;

  /** The user identifier associated with this login event. */
  @NotNull private String userId;

  /** The page or endpoint where the login was initiated. */
  @NotNull
  @Size(max = 4096)
  private String page;

  /** The type of action performed during the login event. */
  private int action;

  /** Additional description or context about the login event. */
  @Size(max = 500)
  private String description;
}
