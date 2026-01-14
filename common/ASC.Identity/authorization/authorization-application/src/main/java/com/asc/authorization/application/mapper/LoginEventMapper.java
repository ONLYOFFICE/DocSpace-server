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

package com.asc.authorization.application.mapper;

import com.asc.authorization.data.login.entity.LoginEventEntity;
import com.asc.common.service.transfer.message.LoginRegisteredEvent;
import org.springframework.stereotype.Component;

/**
 * Mapper component responsible for converting login event data between different representations.
 * This mapper specifically handles the conversion from {@link LoginRegisteredEvent} to {@link
 * LoginEventEntity}.
 *
 * <p>The mapper follows the standard mapping pattern used throughout the application to transform
 * data transfer objects (DTOs) into domain entities for persistence. It is specifically designed to
 * handle login-related events and their conversion to database entities for audit and tracking
 * purposes.
 */
@Component
public class LoginEventMapper {

  /**
   * Converts a {@link LoginRegisteredEvent} to a {@link LoginEventEntity}.
   *
   * <p>This method performs a one-to-one mapping of all fields from the login registered event to
   * the corresponding login event entity. All fields are copied directly without any transformation
   * or validation logic.
   *
   * <p>The mapping includes the following fields:
   *
   * <ul>
   *   <li>Login - The login identifier or username
   *   <li>Active - Boolean flag indicating if the login is active
   *   <li>IP - The IP address from which the login was performed
   *   <li>Browser - The browser information used for the login
   *   <li>Platform - The platform information (e.g., operating system)
   *   <li>Date - The timestamp when the login event occurred
   *   <li>Tenant ID - The tenant identifier associated with the login
   *   <li>User ID - The user identifier who performed the login
   *   <li>Page - The page or endpoint where the login was initiated
   *   <li>Action - The type of login action performed
   *   <li>Description - Additional description or context of the login event
   * </ul>
   *
   * @param loginRegisteredEvent the login registered event to convert, must not be null
   * @return a new {@link LoginEventEntity} instance with all fields mapped from the input event
   * @throws NullPointerException if the loginRegisteredEvent parameter is null
   */
  public LoginEventEntity toEntity(LoginRegisteredEvent loginRegisteredEvent) {
    return LoginEventEntity.builder()
        .login(loginRegisteredEvent.getLogin())
        .active(loginRegisteredEvent.isActive())
        .ip(loginRegisteredEvent.getIp())
        .browser(loginRegisteredEvent.getBrowser())
        .platform(loginRegisteredEvent.getPlatform())
        .date(loginRegisteredEvent.getDate())
        .tenantId(loginRegisteredEvent.getTenantId())
        .userId(loginRegisteredEvent.getUserId())
        .page(loginRegisteredEvent.getPage())
        .action(loginRegisteredEvent.getAction())
        .description(loginRegisteredEvent.getDescription())
        .build();
  }
}
