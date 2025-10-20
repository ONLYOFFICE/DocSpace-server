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
