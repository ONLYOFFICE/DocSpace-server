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

package com.asc.common.messaging.configuration;

import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for RabbitMQ messaging components related to auditing.
 *
 * <p>Defines exchanges, queues, and bindings for handling audit messages and dead-letter messages.
 *
 * <p>This configuration is only loaded when RabbitMQ classes are available on the classpath.
 */
@Configuration
@ConditionalOnClass(Queue.class)
public class RabbitAuditMessagingConfiguration {

  @Value("${spring.application.region}")
  private String region;

  private static final int MAX_QUEUE_SIZE = 10_000;
  private static final int AUDIT_QUEUE_TTL_MS = 30_000;
  private static final int DEAD_LETTER_QUEUE_TTL_MS = 5_000;

  /**
   * Defines the direct exchange for audit messages.
   *
   * @return the {@link DirectExchange} for audit messages.
   */
  @Bean
  public DirectExchange auditExchange() {
    return new DirectExchange("asc_identity_audit_" + region + "_exchange");
  }

  /**
   * Defines the queue for storing audit messages.
   *
   * <p>This queue is configured with a time-to-live (TTL), size limit, and a dead-letter exchange
   * for overflow handling.
   *
   * @return the {@link Queue} for audit messages.
   */
  @Bean
  public Queue auditQueue() {
    return QueueBuilder.durable("asc_identity_audit_" + region.toLowerCase() + "_queue")
        .withArgument("x-single-active-consumer", true)
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-message-ttl", AUDIT_QUEUE_TTL_MS)
        .withArgument("x-overflow", "reject-publish-dlx")
        .withArgument(
            "x-dead-letter-exchange", "asc_identity_audit_" + region.toLowerCase() + "_dlx")
        .withArgument("x-dead-letter-routing-key", Strings.EMPTY)
        .build();
  }

  /**
   * Defines the direct exchange for audit dead-letter messages.
   *
   * @return the {@link DirectExchange} for the audit dead-letter queue.
   */
  @Bean
  public DirectExchange auditDeadLetterExchange() {
    return new DirectExchange("asc_identity_audit_" + region.toLowerCase() + "_dlx");
  }

  /**
   * Defines the queue for storing dead-letter audit messages.
   *
   * <p>This queue is configured with a time-to-live (TTL) and size limit.
   *
   * @return the {@link Queue} for audit dead-letter messages.
   */
  @Bean
  public Queue auditDeadLetterQueue() {
    return QueueBuilder.durable("asc_identity_audit_" + region.toLowerCase() + "_dlq")
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-message-ttl", DEAD_LETTER_QUEUE_TTL_MS)
        .withArgument("x-overflow", "reject-publish")
        .build();
  }

  /**
   * Binds the audit queue to the audit exchange.
   *
   * @return the {@link Binding} between the audit queue and audit exchange.
   */
  @Bean
  public Binding auditQueueBinding() {
    return BindingBuilder.bind(auditQueue()).to(auditExchange()).with(Strings.EMPTY);
  }

  /**
   * Binds the audit dead-letter queue to the audit dead-letter exchange.
   *
   * @return the {@link Binding} between the audit dead-letter queue and audit dead-letter exchange.
   */
  @Bean
  public Binding auditDeadLetterQueueBinding() {
    return BindingBuilder.bind(auditDeadLetterQueue())
        .to(auditDeadLetterExchange())
        .with(Strings.EMPTY);
  }
}
