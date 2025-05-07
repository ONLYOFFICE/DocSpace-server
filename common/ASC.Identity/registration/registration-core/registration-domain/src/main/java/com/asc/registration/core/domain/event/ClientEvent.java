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
