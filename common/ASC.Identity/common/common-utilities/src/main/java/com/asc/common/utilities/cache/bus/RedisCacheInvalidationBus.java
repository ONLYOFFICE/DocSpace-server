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

package com.asc.common.utilities.cache.bus;

import com.asc.common.utilities.cache.MemoryCache;
import java.nio.charset.StandardCharsets;
import java.util.function.Consumer;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.listener.ChannelTopic;
import org.springframework.data.redis.listener.RedisMessageListenerContainer;

/**
 * {@link CacheInvalidationBus} over Redis pub/sub.
 *
 * <p>Pub/sub delivers every message to every connected subscriber, which is what an in-process
 * cache needs, and nothing is kept for subscribers that are disconnected at the time. Instances
 * that miss a message rely on the {@link MemoryCache}'s own expiry.
 *
 * <p>The bus is its own listener container, so registering it as a bean is all it takes for Spring
 * to open the subscription on startup and close it on shutdown.
 */
@Slf4j
public class RedisCacheInvalidationBus extends RedisMessageListenerContainer
    implements CacheInvalidationBus {
  private final StringRedisTemplate redisTemplate;
  private final ChannelTopic topic;

  /**
   * @param connectionFactory the Redis connection to publish and subscribe through.
   * @param channel the pub/sub channel of the cache this bus serves.
   */
  public RedisCacheInvalidationBus(RedisConnectionFactory connectionFactory, String channel) {
    this.redisTemplate = new StringRedisTemplate(connectionFactory);
    this.topic = new ChannelTopic(channel);
    setConnectionFactory(connectionFactory);
  }

  @Override
  public void publish(String message) {
    redisTemplate.convertAndSend(topic.getTopic(), message);
  }

  @Override
  public void subscribe(Consumer<String> listener) {
    addMessageListener(
        (message, _) -> {
          try {
            listener.accept(new String(message.getBody(), StandardCharsets.UTF_8));
          } catch (Exception e) {
            log.warn("Failed to apply cache invalidation from channel {}", topic.getTopic(), e);
          }
        },
        topic);
  }
}
