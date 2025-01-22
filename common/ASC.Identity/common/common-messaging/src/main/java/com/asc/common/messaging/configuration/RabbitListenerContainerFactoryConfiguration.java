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

import com.asc.common.service.transfer.message.AuditMessage;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.Map;
import lombok.Getter;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AcknowledgeMode;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
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
public class RabbitListenerContainerFactoryConfiguration {
  private int prefetch = 500;
  private int batchSize = 20;

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
   * Configures a {@link SimpleRabbitListenerContainerFactory} bean for RabbitMQ listeners with
   * manual acknowledgment. This factory ensures that messages are acknowledged explicitly by the
   * listener, allowing for better control over message processing and error handling. It is
   * configured with a prefetch count of 1 to process one message at a time, reducing the risk of
   * message loss or unprocessed batches.
   *
   * @param connectionFactory the RabbitMQ {@link ConnectionFactory} to be used for establishing
   *     connections.
   * @return a configured {@link SimpleRabbitListenerContainerFactory} with manual acknowledgment
   *     and single-message prefetch.
   */
  @Bean("rabbitSingleManualContainerFactory")
  public SimpleRabbitListenerContainerFactory rabbitSingleManualContainerFactory(
      ConnectionFactory connectionFactory) {
    SimpleRabbitListenerContainerFactory factory = new SimpleRabbitListenerContainerFactory();
    factory.setConnectionFactory(connectionFactory);
    factory.setAcknowledgeMode(AcknowledgeMode.MANUAL);
    factory.setPrefetchCount(1);
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
