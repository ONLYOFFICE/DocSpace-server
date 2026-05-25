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
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.stereotype.Component;

/**
 * RabbitMQ message publisher for broadcasting client removal events.
 *
 * <p>This component implements {@link AuthorizationMessagePublisher} to send {@link
 * ClientRemovedEvent} messages to the specified exchange for authorization cleanup.
 *
 * <p>This publisher is only loaded when RabbitMQ classes are available on the classpath.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class RabbitAuthorizationRemoveMessagePublisher
    implements AuthorizationMessagePublisher<ClientRemovedEvent> {

  private final AmqpTemplate amqpClient;

  /**
   * Publishes a {@link ClientRemovedEvent} to the authorization cleanup exchange.
   *
   * <p>The message is sent to the {@link AuthorizationMessagingConfiguration#ENTRY_EXCHANGE} with
   * an empty routing key.
   *
   * @param message the {@link ClientRemovedEvent} containing details of the client to be removed.
   */
  @Override
  public void publish(ClientRemovedEvent message) {
    log.debug("Broadcasting a remove authorizations event: {}", message);
    amqpClient.convertAndSend(
        AuthorizationMessagingConfiguration.ENTRY_EXCHANGE, Strings.EMPTY, message);
  }
}
