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

package com.asc.common.messaging.configuration;

import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for setting up RabbitMQ messaging components related to authorization.
 *
 * <p>This configuration defines exchanges, queues, and bindings for handling authorization cleanup
 * messages and dead-letter messages.
 */
@Configuration
public class AuthorizationMessagingConfiguration {
  @Value("${spring.application.region}")
  private String region;

  /** The exchange for entry point messages for authorization cleanup. */
  public static final String ENTRY_EXCHANGE = "asc_identity_authorization_cleanup_exchange";

  /** The fanout exchange for distributing messages across regional queues. */
  public static final String FANOUT_EXCHANGE = "asc_identity_authorization_region_exchange";

  /** The dead-letter exchange for handling messages that cannot be processed. */
  public static final String DEAD_LETTER_EXCHANGE = "asc_identity_authorization_region_dlx";

  /** The dead-letter queue for storing messages that have failed processing. */
  public static final String DEAD_LETTER_QUEUE = "asc_identity_authorization_region_dlq";

  /** The entry queue for processing authorization cleanup messages. */
  public static final String ENTRY_QUEUE = "asc_identity_authorization_cleanup_queue";

  private static final int MAX_QUEUE_SIZE = 15_000;
  private static final int ENTRY_QUEUE_TTL_MS = 3000;

  /**
   * Defines the direct exchange for authorization entry messages.
   *
   * @return the {@link DirectExchange} for the entry queue.
   */
  @Bean
  public DirectExchange authorizationEntryExchange() {
    return new DirectExchange(ENTRY_EXCHANGE);
  }

  /**
   * Defines the queue for authorization cleanup messages.
   *
   * <p>This queue has a time-to-live (TTL) and is linked to a dead-letter exchange.
   *
   * @return the {@link Queue} for authorization entry messages.
   */
  @Bean
  public Queue authorizationEntryQueue() {
    return QueueBuilder.durable(ENTRY_QUEUE)
        .withArgument("x-message-ttl", ENTRY_QUEUE_TTL_MS)
        .withArgument("x-dead-letter-exchange", FANOUT_EXCHANGE)
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .build();
  }

  /**
   * Defines the fanout exchange for regional message distribution.
   *
   * @return the {@link FanoutExchange} for regional queues.
   */
  @Bean
  public FanoutExchange authorizationFanoutExchange() {
    return new FanoutExchange(FANOUT_EXCHANGE);
  }

  /**
   * Defines the regional queue for authorization messages.
   *
   * <p>This queue is linked to the dead-letter exchange and has a size limit.
   *
   * @return the {@link Queue} for regional messages.
   */
  @Bean
  public Queue authorizationRegionQueue() {
    return QueueBuilder.durable("asc_identity_authorization_" + region.toLowerCase() + "_queue")
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-overflow", "reject-publish-dlx")
        .withArgument("x-dead-letter-exchange", DEAD_LETTER_EXCHANGE)
        .withArgument("x-dead-letter-routing-key", Strings.EMPTY)
        .build();
  }

  /**
   * Defines the direct exchange for dead-letter messages.
   *
   * @return the {@link DirectExchange} for the dead-letter queue.
   */
  @Bean
  public DirectExchange authorizationDeadLetterExchange() {
    return new DirectExchange(DEAD_LETTER_EXCHANGE);
  }

  /**
   * Defines the queue for storing dead-letter messages.
   *
   * @return the {@link Queue} for dead-letter messages.
   */
  @Bean
  public Queue authorizationDeadLetterQueue() {
    return QueueBuilder.durable(DEAD_LETTER_QUEUE).build();
  }

  /**
   * Binds the entry queue to the entry exchange.
   *
   * @return the {@link Binding} between the entry queue and entry exchange.
   */
  @Bean
  public Binding authorizationEntryQueueBinding() {
    return BindingBuilder.bind(authorizationEntryQueue())
        .to(authorizationEntryExchange())
        .with(Strings.EMPTY);
  }

  /**
   * Binds the regional queue to the fanout exchange.
   *
   * @return the {@link Binding} between the regional queue and fanout exchange.
   */
  @Bean
  public Binding authorizationFanOutBinding() {
    return BindingBuilder.bind(authorizationRegionQueue()).to(authorizationFanoutExchange());
  }

  /**
   * Binds the dead-letter queue to the dead-letter exchange.
   *
   * @return the {@link Binding} between the dead-letter queue and dead-letter exchange.
   */
  @Bean
  public Binding authorizationDeadLetterQueueBinding() {
    return BindingBuilder.bind(authorizationDeadLetterQueue())
        .to(authorizationDeadLetterExchange())
        .with(Strings.EMPTY);
  }
}
