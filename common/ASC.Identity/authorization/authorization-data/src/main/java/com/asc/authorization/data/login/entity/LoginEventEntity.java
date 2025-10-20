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
