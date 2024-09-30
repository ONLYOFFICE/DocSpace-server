// (c) Copyright Ascensio System SIA 2009-2024
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

import com.asc.common.service.transfer.message.AuditMessage;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.HashMap;
import java.util.Map;
import lombok.Getter;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.*;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitAdmin;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.rabbit.listener.RabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.listener.SimpleMessageListenerContainer;
import org.springframework.amqp.support.converter.DefaultJackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for setting up RabbitMQ messaging infrastructure. It includes configuration
 * for queues, exchanges, bindings, message converters, and listener container factories.
 *
 * <p>This class reads properties prefixed with "spring.cloud.messaging.rabbitmq" from the
 * application configuration and sets up the necessary beans.
 */
@Slf4j
@Setter
@Getter
@Configuration
@ConfigurationProperties(prefix = "spring.cloud.messaging.rabbitmq")
public class RabbitMQConfiguration {
  private final Map<String, RabbitMQGenericQueueConfiguration> queues = new HashMap<>();
  private int prefetch = 500;
  private int batchSize = 20;

  /**
   * Bean for creating and configuring a RabbitAdmin instance.
   *
   * @param connectionFactory the RabbitMQ connection factory
   * @return a configured RabbitAdmin instance
   */
  @Bean
  public RabbitAdmin rabbitAdmin(ConnectionFactory connectionFactory) {
    var rabbitAdmin = new RabbitAdmin(connectionFactory);
    queues.forEach(
        (key, value) -> {
          value.validate();

          var builder = QueueBuilder.durable(value.getQueue());
          if (value.isNonDurable()) builder = QueueBuilder.nonDurable(value.getQueue());
          if (value.isAutoDelete()) {
            builder.autoDelete();
          } else
            builder
                .withArgument("x-delivery-limit", value.getDeliveryLimit())
                .withArgument("x-queue-type", "quorum");
          var deadQueueName = value.getDeadQueue();
          var deadExchangeName = value.getDeadExchange();
          var deadRoutingName = value.getDeadRouting();
          if (deadQueueName != null) {
            var deadQueue =
                QueueBuilder.durable(deadQueueName)
                    .withArgument("x-max-length-bytes", value.getDeadMaxBytes())
                    .withArgument("x-message-ttl", value.getMessageTTL())
                    .withArgument("x-queue-type", "quorum")
                    .build();
            var deadExchange = new TopicExchange(deadExchangeName);
            var deadBinding = BindingBuilder.bind(deadQueue).to(deadExchange).with(deadRoutingName);
            builder
                .withArgument("x-dead-letter-exchange", deadExchangeName)
                .withArgument("x-dead-letter-routing-key", deadRoutingName);
            rabbitAdmin.declareQueue(deadQueue);
            rabbitAdmin.declareExchange(deadExchange);
            rabbitAdmin.declareBinding(deadBinding);
          }

          Exchange exchange = new TopicExchange(value.getExchange());
          if (value.isFanOut()) exchange = new FanoutExchange(value.getExchange());
          var queue =
              builder
                  .withArgument("x-max-length-bytes", value.getMaxBytes())
                  .withArgument("x-message-ttl", value.getMessageTTL())
                  .withArgument("x-overflow", "reject-publish")
                  .build();
          var binding = BindingBuilder.bind(queue).to(exchange).with(value.getRouting()).noargs();

          rabbitAdmin.declareQueue(queue);
          rabbitAdmin.declareExchange(exchange);
          rabbitAdmin.declareBinding(binding);
        });

    return rabbitAdmin;
  }

  /**
   * Bean for creating and configuring a Jackson2JsonMessageConverter instance.
   *
   * @param mapper the ObjectMapper to use for JSON conversion
   * @return a configured Jackson2JsonMessageConverter instance
   */
  @Bean
  public MessageConverter jsonMessageConverter(ObjectMapper mapper) {
    log.info("Building a json message converter");

    var messageConverter = new Jackson2JsonMessageConverter(mapper);
    var classMapper = new DefaultJackson2JavaTypeMapper();
    classMapper.setTrustedPackages("*");
    classMapper.setIdClassMapping(Map.of("audit", AuditMessage.class));
    messageConverter.setClassMapper(classMapper);
    messageConverter.setTypePrecedence(Jackson2JavaTypeMapper.TypePrecedence.TYPE_ID);
    return messageConverter;
  }

  /**
   * Bean for creating and configuring a RabbitListenerContainerFactory instance.
   *
   * @param connectionFactory the RabbitMQ connection factory
   * @param converter the message converter to use
   * @return a configured RabbitListenerContainerFactory instance
   */
  @Bean("rabbitListenerContainerFactory")
  public RabbitListenerContainerFactory<?> rabbitFactory(
      ConnectionFactory connectionFactory, MessageConverter converter) {
    log.info("Building a default rabbit listener container factory");

    var factory = new SimpleRabbitListenerContainerFactory();
    factory.setConnectionFactory(connectionFactory);
    factory.setMessageConverter(converter);
    return factory;
  }

  /**
   * Bean for creating and configuring a batch RabbitListenerContainerFactory instance.
   *
   * @param rabbitConnectionFactory the RabbitMQ connection factory
   * @param converter the message converter to use
   * @return a configured SimpleRabbitListenerContainerFactory instance for batch processing
   */
  @Bean("batchRabbitListenerContainerFactory")
  public RabbitListenerContainerFactory<SimpleMessageListenerContainer>
      batchRabbitListenerContainerFactory(
          ConnectionFactory rabbitConnectionFactory, MessageConverter converter) {
    MDC.put("prefetch", String.valueOf(prefetch));
    MDC.put("batch", String.valueOf(batchSize));
    log.info("Building a batch rabbit listener container factory with manual ack");
    MDC.clear();

    var factory = new SimpleRabbitListenerContainerFactory();
    factory.setConnectionFactory(rabbitConnectionFactory);
    factory.setMessageConverter(converter);
    factory.setPrefetchCount(prefetch);
    factory.setAcknowledgeMode(AcknowledgeMode.AUTO);
    factory.setBatchListener(true);
    factory.setBatchSize(batchSize);
    factory.setConsumerBatchEnabled(true);
    return factory;
  }

  /**
   * Creates and configures an AmqpTemplate instance.
   *
   * @param connectionFactory the RabbitMQ connection factory
   * @param converter the message converter to use
   * @return a configured AmqpTemplate instance
   */
  public AmqpTemplate rabbitTemplate(
      ConnectionFactory connectionFactory, MessageConverter converter) {
    log.info("Building an amqp template");

    var rabbitTemplate = new RabbitTemplate(connectionFactory);
    rabbitTemplate.setMessageConverter(converter);
    return rabbitTemplate;
  }
}
