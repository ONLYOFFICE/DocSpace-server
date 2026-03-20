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

import org.springframework.amqp.core.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;

/**
 * Configuration class for setting up RabbitMQ messaging components related to client registration
 * RPC.
 *
 * <p>This configuration defines exchanges, queues, and bindings for handling client retrieval RPC
 * requests in a multi-region SaaS environment. It enables cross-region client lookups for token
 * introspection.
 *
 * <p>This configuration is only loaded when RabbitMQ classes are available on the classpath.
 */
@Configuration
@ConditionalOnClass(Queue.class)
public class ClientRegistrationMessagingConfiguration {
  @Value("${spring.application.region}")
  private String region;

  public static final String CLIENT_RPC_EXCHANGE = "asc_identity_client_rpc_exchange";
  public static final String CLIENT_RPC_ROUTING_KEY_PREFIX = "rpc.";

  private static final int MAX_QUEUE_SIZE = 15_000;
  private static final int QUEUE_TTL_MS = 3000;

  /**
   * Defines the topic exchange for client registration RPC messages.
   *
   * <p>This exchange is only active in the "saas" profile and enables cross-region client lookups
   * in a clustered environment.
   *
   * @return the {@link TopicExchange} for client registration RPC messages
   */
  @Bean
  @Profile("saas")
  public TopicExchange clientRpcExchange() {
    return new TopicExchange(CLIENT_RPC_EXCHANGE);
  }

  /**
   * Defines the region-specific queue for client registration RPC requests.
   *
   * <p>Only active in the "saas" profile.
   *
   * @return the {@link Queue} for client registration RPC requests
   */
  @Bean
  @Profile("saas")
  public Queue clientRpcQueue() {
    return QueueBuilder.durable("asc_identity_client_rpc_" + region.toLowerCase() + "_queue")
        .withArgument("x-message-ttl", QUEUE_TTL_MS)
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-overflow", "reject-publish")
        .build();
  }

  /**
   * Binds the RPC queue to the RPC exchange with a region-specific routing key.
   *
   * <p>Messages are routed using the pattern "rpc.{region}" (e.g., "rpc.eu"). Only active in the
   * "saas" profile.
   *
   * @return the {@link Binding} between the RPC queue and RPC exchange
   */
  @Bean
  @Profile("saas")
  public Binding clientRpcQueueBinding() {
    return BindingBuilder.bind(clientRpcQueue())
        .to(clientRpcExchange())
        .with(CLIENT_RPC_ROUTING_KEY_PREFIX + region.toLowerCase());
  }
}
