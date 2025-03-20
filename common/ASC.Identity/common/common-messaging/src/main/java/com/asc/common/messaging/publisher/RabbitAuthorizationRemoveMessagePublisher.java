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

package com.asc.common.messaging.publisher;

import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Component;

/**
 * RabbitMQ message publisher for broadcasting client removal events.
 *
 * <p>This component implements {@link AuthorizationMessagePublisher} to send {@link
 * ClientRemovedEvent} messages to the specified exchange for authorization cleanup.
 */
@Slf4j
@Component
@RequiredArgsConstructor
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
