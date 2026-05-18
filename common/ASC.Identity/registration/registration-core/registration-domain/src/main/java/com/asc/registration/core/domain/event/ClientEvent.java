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

package com.asc.registration.core.domain.event;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEvent;
import com.asc.registration.core.domain.entity.Client;
import java.time.ZonedDateTime;

/**
 * ClientEvent is an abstract class that represents a domain event associated with a Client. It
 * encapsulates audit information, the client involved in the event, and the timestamp of the event.
 */
public abstract class ClientEvent implements DomainEvent<Client> {
  private final Audit audit;
  private final Client client;
  private final ZonedDateTime eventAt;

  /**
   * Constructs a ClientEvent with the specified audit information, client, and event timestamp.
   *
   * @param audit the audit information related to the event
   * @param client the client involved in the event
   * @param eventAt the timestamp of the event
   */
  public ClientEvent(Audit audit, Client client, ZonedDateTime eventAt) {
    this.audit = audit;
    this.client = client;
    this.eventAt = eventAt;
  }

  /**
   * Returns the audit information associated with this event.
   *
   * @return the audit information
   */
  public Audit getAudit() {
    return this.audit;
  }

  /**
   * Returns the client involved in this event.
   *
   * @return the client
   */
  public Client getClient() {
    return this.client;
  }

  /**
   * Returns the timestamp of this event.
   *
   * @return the event timestamp
   */
  public ZonedDateTime getEventAt() {
    return this.eventAt;
  }
}
