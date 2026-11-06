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

import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.AmqpTemplate;

/**
 * Publishes a client cache eviction to every region, and at once to the authorization service of
 * the region it originates from.
 *
 * <p>An eviction is sent once and never kept: one lost to a broker outage or to the process dying
 * before the send leaves the cached client in place until its entry expires on its own.
 *
 * @param <E> the type of the eviction message
 */
@Slf4j
public abstract class ClientCacheEvictionPublisher<E> implements AuthorizationMessagePublisher<E> {
  private final AmqpTemplate template;
  private final String localAuthorizationQueue;
  private final RabbitBackgroundPublisher background;

  /**
   * @param template the AMQP template to publish with
   * @param region the region this service runs in
   * @param background runs the sends off the caller's thread
   */
  protected ClientCacheEvictionPublisher(
      AmqpTemplate template, String region, RabbitBackgroundPublisher background) {
    this.template = template;
    this.background = background;
    this.localAuthorizationQueue =
        ClientCacheMessagingConfiguration.authorizationRegionQueue(region);
  }

  private void broadcast(E message) {
    template.convertAndSend(
        ClientCacheMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);

    try {
      template.convertAndSend(Strings.EMPTY, localAuthorizationQueue, message);
    } catch (Exception e) {
      log.warn("Could not evict the local authorization cache at once", e);
    }
  }

  @Override
  public void publish(E message) {
    log.debug("Broadcasting a client cache eviction: {}", message);
    background.run(message, () -> broadcast(message));
  }
}
