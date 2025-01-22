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

package com.asc.authorization.messaging.listener;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.rabbitmq.client.Channel;
import java.io.IOException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.TransactionDefinition;
import org.springframework.transaction.support.TransactionTemplate;

/**
 * Listener component for processing messages related to client removal events.
 *
 * <p>This component listens to RabbitMQ messages on a specified queue, parses the {@link
 * ClientRemovedEvent}, and performs cleanup operations by deleting associated authorizations and
 * consents for the client.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationMessagingCleanupListener {
  private final ObjectMapper objectMapper =
      new ObjectMapper()
          .setSerializationInclusion(JsonInclude.Include.NON_NULL)
          .configure(DeserializationFeature.FAIL_ON_NULL_FOR_PRIMITIVES, true)
          .configure(DeserializationFeature.FAIL_ON_MISSING_CREATOR_PROPERTIES, true);

  private final PlatformTransactionManager transactionManager;
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final JpaConsentRepository jpaConsentRepository;

  /**
   * Listens for messages on the specified RabbitMQ queue and processes client removal events.
   *
   * <p>Deletes all authorizations and consents associated with the client ID in the received {@link
   * ClientRemovedEvent}. Handles message acknowledgment and transaction management.
   *
   * @param message the incoming RabbitMQ message containing the client removal event data.
   * @param channel the RabbitMQ channel used for manual message acknowledgment.
   * @throws IOException if there is an error reading or processing the message.
   */
  @RabbitListener(
      queues = "asc_identity_authorization_${spring.application.region}_queue",
      containerFactory = "rabbitSingleManualContainerFactory")
  public void receiveMessage(@Payload Message message, Channel channel) throws IOException {
    try {
      var event = objectMapper.readValue(message.getBody(), ClientRemovedEvent.class);
      MDC.put("client_id", event.getClientId());

      var template = new TransactionTemplate(transactionManager);
      template.setIsolationLevel(TransactionDefinition.ISOLATION_READ_COMMITTED);
      template.setTimeout(2);
      template.execute(
          status -> {
            try {
              log.info("Removing authorizations and consents for client: {}", event.getClientId());

              jpaAuthorizationRepository.deleteAllAuthorizationsByClientId(event.getClientId());
              jpaConsentRepository.deleteAllConsentsByClientId(event.getClientId());

              log.info(
                  "Authorizations and consents for client {} have been removed",
                  event.getClientId());
              return null;
            } catch (Exception ex) {
              log.warn(
                  "Failed to remove authorizations and consents for client {}",
                  event.getClientId(),
                  ex);
              status.setRollbackOnly();
              throw ex;
            }
          });

      channel.basicAck(message.getMessageProperties().getDeliveryTag(), false);
    } catch (IOException e) {
      log.warn("Received an unsupported message format: {}", e.getMessage());
      channel.basicAck(message.getMessageProperties().getDeliveryTag(), false);
    } catch (Exception e) {
      log.error("Error processing client removal event: {}", e.getMessage(), e);
      channel.basicNack(message.getMessageProperties().getDeliveryTag(), false, false);
    } finally {
      MDC.clear();
    }
  }
}
