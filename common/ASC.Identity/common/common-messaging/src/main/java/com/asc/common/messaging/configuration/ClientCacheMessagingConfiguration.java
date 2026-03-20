// (c) Copyright Ascensio System SIA 2009-2026
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
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;

/**
 * Configuration class for setting up RabbitMQ messaging components related to client cache cleanup.
 *
 * <p>This configuration defines exchanges, queues, and bindings for handling client cache cleanup
 * messages across multiple regions. It follows a multi-region pattern where messages are first sent
 * to an entry exchange, then distributed via a fanout exchange to regional queues.
 *
 * <p>This configuration is only loaded when RabbitMQ classes are available on the classpath.
 */
@Configuration
@Profile("saas")
@ConditionalOnClass(Queue.class)
public class ClientCacheMessagingConfiguration {
  @Value("${spring.application.region}")
  private String region;

  /** The exchange for entry point messages for client cache cleanup. */
  public static final String ENTRY_EXCHANGE = "asc_identity_client_cache_cleanup_exchange";

  /** The fanout exchange for distributing messages across regional queues. */
  public static final String FANOUT_EXCHANGE = "asc_identity_client_cache_region_exchange";

  /** The dead-letter exchange for handling messages that cannot be processed. */
  public static final String DEAD_LETTER_EXCHANGE = "asc_identity_client_cache_region_dlx";

  /** The dead-letter queue for storing messages that have failed processing. */
  public static final String DEAD_LETTER_QUEUE = "asc_identity_client_cache_region_dlq";

  /** The entry queue for processing client cache cleanup messages. */
  public static final String ENTRY_QUEUE = "asc_identity_client_cache_cleanup_queue";

  private static final int MAX_QUEUE_SIZE = 15_000;
  private static final int QUEUE_TTL_MS = 3000;

  /**
   * Defines the direct exchange for client cache cleanup entry messages.
   *
   * @return the {@link DirectExchange} for the entry queue.
   */
  @Bean
  public DirectExchange clientCacheEntryExchange() {
    return new DirectExchange(ENTRY_EXCHANGE);
  }

  /**
   * Defines the queue for client cache cleanup messages.
   *
   * <p>This queue has a time-to-live (TTL) and is linked to a dead-letter exchange. Messages that
   * expire or cannot be processed are forwarded to the fanout exchange for regional distribution.
   *
   * @return the {@link Queue} for client cache cleanup entry messages.
   */
  @Bean
  public Queue clientCacheEntryQueue() {
    return QueueBuilder.durable(ENTRY_QUEUE)
        .withArgument("x-message-ttl", QUEUE_TTL_MS)
        .withArgument("x-dead-letter-exchange", FANOUT_EXCHANGE)
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .build();
  }

  /**
   * Defines the fanout exchange for regional message distribution.
   *
   * <p>This exchange distributes client cache cleanup messages to all regional queues, ensuring
   * that cache invalidation is propagated across all regions.
   *
   * @return the {@link FanoutExchange} for regional queues.
   */
  @Bean
  public FanoutExchange clientCacheFanoutExchange() {
    return new FanoutExchange(FANOUT_EXCHANGE);
  }

  /**
   * Defines the regional queue for client cache cleanup messages.
   *
   * <p>This queue is linked to the dead-letter exchange and has a size limit. Each region has its
   * own queue to process cache cleanup messages locally.
   *
   * @return the {@link Queue} for regional client cache cleanup messages.
   */
  @Bean
  public Queue clientCacheRegionQueue() {
    return QueueBuilder.durable("asc_identity_client_cache_" + region.toLowerCase() + "_queue")
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-overflow", "reject-publish-dlx")
        .withArgument("x-dead-letter-exchange", DEAD_LETTER_EXCHANGE)
        .withArgument("x-dead-letter-routing-key", Strings.EMPTY)
        .build();
  }

  /**
   * Defines the direct exchange for dead-letter messages.
   *
   * <p>Messages that cannot be processed or exceed queue limits are routed to this exchange and
   * eventually to the dead-letter queue for manual inspection.
   *
   * @return the {@link DirectExchange} for the dead-letter queue.
   */
  @Bean
  public DirectExchange clientCacheDeadLetterExchange() {
    return new DirectExchange(DEAD_LETTER_EXCHANGE);
  }

  /**
   * Defines the queue for storing dead-letter messages.
   *
   * <p>This queue stores messages that have failed processing or exceeded queue limits, allowing
   * for manual inspection and potential reprocessing.
   *
   * @return the {@link Queue} for dead-letter messages.
   */
  @Bean
  public Queue clientCacheDeadLetterQueue() {
    return QueueBuilder.durable(DEAD_LETTER_QUEUE).build();
  }

  /**
   * Binds the entry queue to the entry exchange.
   *
   * <p>This binding routes client cache cleanup messages from the entry exchange to the entry queue
   * for initial processing.
   *
   * @return the {@link Binding} between the entry queue and entry exchange.
   */
  @Bean
  public Binding clientCacheEntryQueueBinding() {
    return BindingBuilder.bind(clientCacheEntryQueue())
        .to(clientCacheEntryExchange())
        .with(Strings.EMPTY);
  }

  /**
   * Binds the regional queue to the fanout exchange.
   *
   * <p>This binding ensures that cache cleanup messages are distributed to all regional queues,
   * allowing each region to process cache invalidation independently.
   *
   * @return the {@link Binding} between the regional queue and fanout exchange.
   */
  @Bean
  public Binding clientCacheFanOutBinding() {
    return BindingBuilder.bind(clientCacheRegionQueue()).to(clientCacheFanoutExchange());
  }

  /**
   * Binds the dead-letter queue to the dead-letter exchange.
   *
   * <p>This binding routes failed or rejected messages to the dead-letter queue for manual
   * inspection.
   *
   * @return the {@link Binding} between the dead-letter queue and dead-letter exchange.
   */
  @Bean
  public Binding clientCacheDeadLetterQueueBinding() {
    return BindingBuilder.bind(clientCacheDeadLetterQueue())
        .to(clientCacheDeadLetterExchange())
        .with(Strings.EMPTY);
  }
}
