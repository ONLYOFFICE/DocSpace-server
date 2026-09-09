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

package com.asc.authorization.messaging.configuration;

import com.asc.common.messaging.configuration.ClientCacheMessagingConfiguration;
import org.apache.logging.log4j.util.Strings;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.FanoutExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;

/**
 * Configuration class binding a dedicated, per-region queue for the authorization service to the
 * client cache fanout exchange declared by {@link ClientCacheMessagingConfiguration}.
 */
@Configuration
@Profile("saas")
@ConditionalOnClass(Queue.class)
public class RegisteredClientCacheMessagingConfiguration {
  private static final int MAX_QUEUE_SIZE = 15_000;
  private static final int MESSAGE_TTL_MS = 180_000;

  @Value("${spring.application.region}")
  private String region;

  /**
   * Defines the authorization service's per-region queue for client cache cleanup messages.
   *
   * @return the {@link Queue} for regional client cache cleanup messages.
   */
  @Bean
  public Queue authorizationClientCacheRegionQueue() {
    return QueueBuilder.durable(
            "asc_identity_authorization_client_cache_" + region.toLowerCase() + "_queue")
        .withArgument("x-max-length", MAX_QUEUE_SIZE)
        .withArgument("x-overflow", "reject-publish-dlx")
        .withArgument("x-message-ttl", MESSAGE_TTL_MS)
        .withArgument(
            "x-dead-letter-exchange", ClientCacheMessagingConfiguration.DEAD_LETTER_EXCHANGE)
        .withArgument("x-dead-letter-routing-key", Strings.EMPTY)
        .build();
  }

  /**
   * References the shared fanout exchange declared by {@link ClientCacheMessagingConfiguration}.
   *
   * @return the {@link FanoutExchange} used to distribute client cache cleanup messages.
   */
  @Bean
  public FanoutExchange authorizationClientCacheFanoutExchange() {
    return new FanoutExchange(ClientCacheMessagingConfiguration.FANOUT_EXCHANGE);
  }

  /**
   * Binds the authorization service's regional queue to the shared fanout exchange.
   *
   * @return the {@link Binding} between the regional queue and the fanout exchange.
   */
  @Bean
  public Binding authorizationClientCacheFanOutBinding() {
    return BindingBuilder.bind(authorizationClientCacheRegionQueue())
        .to(authorizationClientCacheFanoutExchange());
  }
}
