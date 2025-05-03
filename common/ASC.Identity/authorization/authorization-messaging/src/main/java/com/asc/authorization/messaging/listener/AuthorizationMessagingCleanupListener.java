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
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.rabbitmq.client.Channel;
import java.io.IOException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.rabbit.annotation.RabbitHandler;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
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
@RabbitListener(
    queues = "asc_identity_authorization_${spring.application.region}_queue",
    containerFactory = "rabbitSingleManualContainerFactory")
public class AuthorizationMessagingCleanupListener {
  /** Transaction manager for handling database transactions. */
  private final PlatformTransactionManager transactionManager;

  /** Repository for managing authorization entities. */
  private final JpaAuthorizationRepository jpaAuthorizationRepository;

  /** Repository for managing consent entities. */
  private final JpaConsentRepository jpaConsentRepository;

  /**
   * Handles message processing with transaction management and error handling.
   *
   * @param deliveryTag the delivery tag for the message
   * @param channel the RabbitMQ channel
   * @param operation the operation to perform within a transaction
   * @param entityType the type of entity being processed (client, user, tenant)
   * @throws IOException if an I/O error occurs during message handling
   */
  private void handleMessage(
      long deliveryTag, Channel channel, Runnable operation, String entityType) throws IOException {
    try {
      var template = new TransactionTemplate(transactionManager);
      template.setIsolationLevel(TransactionDefinition.ISOLATION_READ_COMMITTED);
      template.setTimeout(2);
      template.execute(
          status -> {
            try {
              operation.run();
              return null;
            } catch (Exception ex) {
              log.warn("Failed to remove authorizations and consents for {}", entityType, ex);
              status.setRollbackOnly();
              throw ex;
            }
          });

      channel.basicAck(deliveryTag, false);
    } catch (IOException e) {
      log.warn("Received an unsupported message format: {}", e.getMessage());
      channel.basicAck(deliveryTag, false);
    } catch (Exception e) {
      log.error("Error processing removal event: {}", e.getMessage(), e);
      channel.basicNack(deliveryTag, false, false);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Handles messages for client removal events.
   *
   * @param event the client removed event
   * @param channel the RabbitMQ channel
   * @param deliveryTag the delivery tag for the message
   * @throws IOException if an I/O error occurs during message handling
   */
  @RabbitHandler
  public void receiveClientRemovedMessage(
      ClientRemovedEvent event, Channel channel, @Header(AmqpHeaders.DELIVERY_TAG) long deliveryTag)
      throws IOException {
    MDC.put("client_id", event.getClientId());
    handleMessage(
        deliveryTag,
        channel,
        () -> {
          log.info("Removing authorizations and consents for client: {}", event.getClientId());
          jpaAuthorizationRepository.deleteAllAuthorizationsByClientId(event.getClientId());
          jpaConsentRepository.deleteAllConsentsByClientId(event.getClientId());
          log.info(
              "Authorizations and consents for client {} have been removed", event.getClientId());
        },
        "client");
  }

  /**
   * Handles messages for user clients removal events.
   *
   * @param event the user clients removed event
   * @param channel the RabbitMQ channel
   * @param deliveryTag the delivery tag for the message
   * @throws IOException if an I/O error occurs during message handling
   */
  @RabbitHandler
  public void receiveUserClientsRemovedMessage(
      @Payload UserClientsRemovedEvent event,
      Channel channel,
      @Header(AmqpHeaders.DELIVERY_TAG) long deliveryTag)
      throws IOException {
    MDC.put("user_id", event.getUserId());
    handleMessage(
        deliveryTag,
        channel,
        () -> {
          log.info("Removing authorizations and consents for user: {}", event.getUserId());
          jpaAuthorizationRepository.deleteAllAuthorizationsByPrincipalId(event.getUserId());
          jpaConsentRepository.deleteAllConsentsByPrincipalId(event.getUserId());
          log.info("Authorizations and consents for user {} have been removed", event.getUserId());
        },
        "user");
  }

  /**
   * Handles messages for tenant clients removal events.
   *
   * @param event the tenant clients removed event
   * @param channel the RabbitMQ channel
   * @param deliveryTag the delivery tag for the message
   * @throws IOException if an I/O error occurs during message handling
   */
  @RabbitHandler
  public void receiveTenantClientsRemovedMessage(
      @Payload TenantClientsRemovedEvent event,
      Channel channel,
      @Header(AmqpHeaders.DELIVERY_TAG) long deliveryTag)
      throws IOException {
    handleMessage(
        deliveryTag,
        channel,
        () -> {
          log.info("Removing authorizations and consents for tenant: {}", event.getTenantId());
          jpaConsentRepository.deleteAllConsentsByTenantId(event.getTenantId());
          jpaAuthorizationRepository.deleteAllAuthorizationsByTenantId(event.getTenantId());
          log.info(
              "Authorizations and consents for tenant {} have been removed", event.getTenantId());
        },
        "tenant");
  }

  /**
   * Default handler for messages that don't match any of the other handlers. Simply acknowledges
   * the message without processing.
   *
   * @param message the raw message object
   * @param channel the RabbitMQ channel
   * @param deliveryTag the delivery tag for the message
   * @throws IOException if an I/O error occurs during message handling
   */
  @RabbitHandler(isDefault = true)
  public void receiveInvalidMessages(
      Object message, Channel channel, @Header(AmqpHeaders.DELIVERY_TAG) long deliveryTag)
      throws IOException {
    channel.basicAck(deliveryTag, false);
  }
}
