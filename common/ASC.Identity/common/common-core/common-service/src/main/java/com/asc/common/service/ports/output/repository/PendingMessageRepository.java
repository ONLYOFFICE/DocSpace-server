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

package com.asc.common.service.ports.output.repository;

import java.util.List;

/**
 * Keeps messages the broker could not take until they are sent again.
 *
 * <p>A message is kept when the broker does not confirm it and stays until a relay manages to send
 * it, so that a broker outage does not lose it.
 */
public interface PendingMessageRepository {

  /**
   * A message waiting to be sent again.
   *
   * @param id the row identifier
   * @param exchange the exchange the message was published to
   * @param routingKey the routing key it was published with
   * @param headers its AMQP headers, as JSON
   * @param body its body
   * @param attempts how many times sending it again has failed
   * @param ageSeconds how long ago it was kept, in seconds
   */
  record PendingMessage(
      long id,
      String exchange,
      String routingKey,
      String headers,
      byte[] body,
      int attempts,
      long ageSeconds) {}

  /**
   * Keeps a message the broker could not take; it is due at once.
   *
   * @param exchange the exchange the message was published to
   * @param routingKey the routing key it was published with
   * @param headers its AMQP headers, as JSON
   * @param body its body
   */
  void save(String exchange, String routingKey, String headers, byte[] body);

  /**
   * Returns the messages that are due, oldest first.
   *
   * @param limit the maximum number of messages to return
   * @return the due messages
   */
  List<PendingMessage> findDue(int limit);

  /**
   * Removes a message once it has been sent.
   *
   * @param id the row identifier
   */
  void delete(long id);

  /**
   * Records a failed attempt and postpones the next one.
   *
   * @param id the row identifier
   * @param delaySeconds how long to wait before the next attempt
   */
  void postpone(long id, long delaySeconds);

  /**
   * Counts the messages waiting to be sent again.
   *
   * @return the number of pending messages
   */
  long count();
}
