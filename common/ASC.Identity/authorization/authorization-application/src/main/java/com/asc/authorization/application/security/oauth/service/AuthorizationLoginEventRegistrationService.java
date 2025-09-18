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

package com.asc.authorization.application.security.oauth.service;

import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.common.service.transfer.message.LoginRegisteredEvent;

/**
 * Service interface for registering login events in the authorization system. This service handles
 * the registration and processing of login events along with their associated audit information for
 * tracking and security purposes.
 *
 * <p>The service is responsible for coordinating the registration of login events that occur during
 * the OAuth authentication process, ensuring that both the login event data and audit trail are
 * properly captured and stored.
 *
 * <p>This interface follows the service layer pattern and provides a clean abstraction for login
 * event registration functionality that can be implemented by different concrete classes based on
 * specific requirements.
 */
public interface AuthorizationLoginEventRegistrationService {

  /**
   * Registers a login event along with its associated audit information.
   *
   * <p>This method processes and stores both the login event data and audit message to maintain a
   * comprehensive record of user authentication activities. The registration typically involves:
   *
   * <ul>
   *   <li>Converting the login event to a persistent entity
   *   <li>Processing the audit message for compliance and tracking
   *   <li>Storing both records in the appropriate data stores
   *   <li>Ensuring data consistency between login and audit records
   * </ul>
   *
   * <p>The implementation should handle any necessary validation, transformation, and persistence
   * operations required to properly register the login event and maintain audit trail integrity.
   *
   * @param loginEvent the login event to register, must not be null
   * @param auditMessage the associated audit message containing audit trail information, must not
   *     be null
   * @throws IllegalArgumentException if either parameter is null or contains invalid data
   * @throws RuntimeException if the registration process fails due to system errors
   */
  void registerLogin(LoginRegisteredEvent loginEvent, AuditMessage auditMessage);
}
