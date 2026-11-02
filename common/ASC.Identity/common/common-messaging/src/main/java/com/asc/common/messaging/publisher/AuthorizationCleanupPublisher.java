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

package com.asc.common.messaging.publisher;

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;

/**
 * Publishes a cleanup of authorizations and consents for every region after the usual short delay.
 * A cleanup that may race requests still in flight is published once more for a run a minute later,
 * which removes grants those requests wrote after the first run. Every message goes through {@link
 * RabbitPersistableMessagePublisher}, so a broker outage delays the cleanup instead of losing it.
 *
 * @param <E> the type of the cleanup event
 */
@Slf4j
public abstract class AuthorizationCleanupPublisher<E> implements AuthorizationMessagePublisher<E> {
  private final RabbitPersistableMessagePublisher publisher;
  private final RabbitBackgroundPublisher background;
  private final boolean runAgainLater;

  /**
   * @param publisher the publisher that keeps messages the broker could not take
   * @param background runs the sends off the caller's thread
   * @param runAgainLater whether to publish the delayed second run too; only a cleanup scoped to
   *     the removed clients may run again, since one scoped to a user or a tenant would also remove
   *     grants made after it
   */
  protected AuthorizationCleanupPublisher(
      RabbitPersistableMessagePublisher publisher,
      RabbitBackgroundPublisher background,
      boolean runAgainLater) {
    this.publisher = publisher;
    this.background = background;
    this.runAgainLater = runAgainLater;
  }

  @Override
  public void publish(E message) {
    log.debug("Broadcasting a cleanup event: {}", message);
    background.run(
        message,
        () ->
            publisher.send(
                AuthorizationMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message));
    if (!runAgainLater) return;
    background.run(
        message,
        () ->
            publisher.send(
                AuthorizationMessagingConfiguration.DELAYED_ENTRY_EXCHANGE,
                Strings.EMPTY,
                message));
  }
}
