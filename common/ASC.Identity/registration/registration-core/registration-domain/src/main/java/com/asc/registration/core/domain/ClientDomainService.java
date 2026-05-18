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

package com.asc.registration.core.domain;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientWebsiteInfo;

/**
 * Defines the domain service interface for managing clients. This service provides methods for
 * creating, updating, enabling, disabling, invalidating, and modifying clients.
 */
public interface ClientDomainService {

  /**
   * Creates a new client.
   *
   * @param audit the audit information related to the creation
   * @param client the client to be created
   * @return a {@link ClientCreatedEvent} indicating the client was created
   */
  ClientCreatedEvent createClient(Audit audit, Client client);

  /**
   * Makes an existing client public.
   *
   * @param audit the audit information related to the update
   * @param client the client to be made public
   * @return a {@link ClientUpdatedEvent} indicating the client was made public
   */
  ClientUpdatedEvent makeClientPublic(Audit audit, Client client);

  /**
   * Makes an existing client private.
   *
   * @param audit the audit information related to the update
   * @param client the client to be made private
   * @return a {@link ClientUpdatedEvent} indicating the client was made private
   */
  ClientUpdatedEvent makeClientPrivate(Audit audit, Client client);

  /**
   * Enables an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be enabled
   * @return a {@link ClientUpdatedEvent} indicating the client was enabled
   */
  ClientUpdatedEvent enableClient(Audit audit, Client client);

  /**
   * Disables an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be disabled
   * @return a {@link ClientUpdatedEvent} indicating the client was disabled
   */
  ClientUpdatedEvent disableClient(Audit audit, Client client);

  /**
   * Deletes an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be invalidated
   * @return a {@link ClientDeletedEvent} indicating the client was invalidated
   */
  ClientDeletedEvent deleteClient(Audit audit, Client client);

  /**
   * Regenerates the secret for an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client whose secret is to be regenerated
   * @return a {@link ClientUpdatedEvent} indicating the client's secret was regenerated
   */
  ClientUpdatedEvent regenerateClientSecret(Audit audit, Client client);

  /**
   * Updates the information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientInfo the new client information
   * @return a {@link ClientUpdatedEvent} indicating the client information was updated
   */
  ClientUpdatedEvent updateClientInfo(Audit audit, Client client, ClientInfo clientInfo);

  /**
   * Updates the website information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientWebsiteInfo the new client website information
   * @return a {@link ClientUpdatedEvent} indicating the client website information was updated
   */
  ClientUpdatedEvent updateClientWebsiteInfo(
      Audit audit, Client client, ClientWebsiteInfo clientWebsiteInfo);

  /**
   * Updates the redirect information of an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param clientRedirectInfo the new client redirect information
   * @return a {@link ClientUpdatedEvent} indicating the client redirect information was updated
   */
  ClientUpdatedEvent updateClientRedirectInfo(
      Audit audit, Client client, ClientRedirectInfo clientRedirectInfo);

  /**
   * Adds an authentication method to an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param authenticationMethod the authentication method to add
   * @return a {@link ClientUpdatedEvent} indicating the authentication method was added to the
   *     client
   */
  ClientUpdatedEvent addAuthenticationMethod(
      Audit audit, Client client, AuthenticationMethod authenticationMethod);

  /**
   * Removes an authentication method from an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param authenticationMethod the authentication method to remove
   * @return a {@link ClientUpdatedEvent} indicating the authentication method was removed from the
   *     client
   */
  ClientUpdatedEvent removeAuthenticationMethod(
      Audit audit, Client client, AuthenticationMethod authenticationMethod);

  /**
   * Adds a scope to an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param scope the scope to add
   * @return a {@link ClientUpdatedEvent} indicating the scope was added to the client
   */
  ClientUpdatedEvent addScope(Audit audit, Client client, String scope);

  /**
   * Removes a scope from an existing client.
   *
   * @param audit the audit information related to the update
   * @param client the client to be updated
   * @param scope the scope to remove
   * @return a {@link ClientUpdatedEvent} indicating the scope was removed from the client
   */
  ClientUpdatedEvent removeScope(Audit audit, Client client, String scope);
}
