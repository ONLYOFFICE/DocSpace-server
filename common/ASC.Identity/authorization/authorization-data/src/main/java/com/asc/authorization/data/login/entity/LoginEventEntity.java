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

package com.asc.authorization.data.login.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Entity class representing a login event in the authorization system. This class is mapped to the
 * `login_events` table and stores data related to user login activities for audit and tracking
 * purposes.
 */
@Entity
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "login_events")
@EqualsAndHashCode
@ToString
public class LoginEventEntity {

  /** The unique identifier for the login event. */
  @Id private long id;

  /**
   * The login identifier or username associated with this event. This field is required and cannot
   * be null.
   */
  @Column(nullable = false)
  private String login;

  /** Flag indicating whether the login session is currently active. */
  private boolean active;

  /**
   * The IP address from which the login was performed. This field is required and cannot be null.
   */
  @Column(nullable = false)
  private String ip;

  /**
   * Information about the browser used for the login. This field is required and cannot be null.
   */
  @Column(nullable = false)
  private String browser;

  /**
   * Information about the platform (operating system) used for the login. This field is required
   * and cannot be null.
   */
  @Column(nullable = false)
  private String platform;

  /** The timestamp when the login event occurred. This field is required and cannot be null. */
  @Column(nullable = false)
  private ZonedDateTime date;

  /**
   * The tenant identifier associated with this login event. This field is required and cannot be
   * null.
   */
  @Column(nullable = false, name = "tenant_id")
  private long tenantId;

  /**
   * The user identifier associated with this login event. This field is required and cannot be
   * null.
   */
  @Column(nullable = false, name = "user_id")
  private String userId;

  /** The page or endpoint where the login was initiated. */
  private String page;

  /** The type of action performed during the login event. */
  private int action;

  /** Additional description or context about the login event. */
  private String description;
}
