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
