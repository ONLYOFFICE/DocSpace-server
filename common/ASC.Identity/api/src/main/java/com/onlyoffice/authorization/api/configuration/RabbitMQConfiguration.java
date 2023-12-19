/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuthorizationMessage;
import com.onlyoffice.authorization.api.web.server.messaging.messages.ConsentMessage;
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

import java.util.HashMap;
import java.util.Map;

/**
 *
 */
@Slf4j
@Setter
@Getter
@Configuration
@ConfigurationProperties(prefix = "spring.cloud.messaging.rabbitmq")
public class RabbitMQConfiguration {
    private final Map<String, GenericQueueConfiguration> queues = new HashMap<>();
    private int prefetch = 500;

    @Bean
    public RabbitAdmin rabbitAdmin(ConnectionFactory connectionFactory) {
        var rabbitAdmin = new RabbitAdmin(connectionFactory);
        queues.forEach((key, value) -> {
            value.validate();

            var builder = QueueBuilder.durable(value.getQueue());
            if (value.isNonDurable())
                builder = QueueBuilder.nonDurable(value.getQueue());
            if (value.isAutoDelete())
                builder = builder.autoDelete();
            else
                builder
                        .withArgument("x-delivery-limit", value.getDeliveryLimit())
                        .withArgument("x-queue-type", "quorum");
            var deadQueueName = value.getDeadQueue();
            var deadExchangeName = value.getDeadExchange();
            var deadRoutingName = value.getDeadRouting();
            if (deadQueueName != null) {
                var deadQueue = QueueBuilder.durable(deadQueueName)
                        .withArgument("x-max-length-bytes", value.getDeadMaxBytes())
                        .withArgument("x-message-ttl", value.getMessageTTL())
                        .withArgument("x-queue-type", "quorum")
                        .build();
                var deadExchange = new TopicExchange(deadExchangeName);
                var deadBinding = BindingBuilder.bind(deadQueue)
                        .to(deadExchange)
                        .with(deadRoutingName);
                builder
                        .withArgument("x-dead-letter-exchange", deadExchangeName)
                        .withArgument("x-dead-letter-routing-key", deadRoutingName);
                rabbitAdmin.declareQueue(deadQueue);
                rabbitAdmin.declareExchange(deadExchange);
                rabbitAdmin.declareBinding(deadBinding);
            }

            Exchange exchange = new TopicExchange(value.getExchange());
            if (value.isFanOut())
                exchange = new FanoutExchange(value.getExchange());
            var queue = builder
                    .withArgument("x-max-length-bytes", value.getMaxBytes())
                    .withArgument("x-message-ttl", value.getMessageTTL())
                    .withArgument("x-overflow", "reject-publish")
                    .build();
            var binding = BindingBuilder.bind(queue)
                    .to(exchange)
                    .with(value.getRouting())
                    .noargs();
            
            rabbitAdmin.declareQueue(queue);
            rabbitAdmin.declareExchange(exchange);
            rabbitAdmin.declareBinding(binding);
        });

        return rabbitAdmin;
    }

    @Bean
    public MessageConverter jsonMessageConverter(ObjectMapper mapper) {
        log.info("Building a json message converter");
        var messageConverter = new Jackson2JsonMessageConverter(mapper);
        var classMapper = new DefaultJackson2JavaTypeMapper();
        classMapper.setTrustedPackages("*");
        classMapper.setIdClassMapping(Map.of(
                "authorization", AuthorizationMessage.class,
                "consent", ConsentMessage.class
        ));
        messageConverter.setClassMapper(classMapper);
        messageConverter.setTypePrecedence(Jackson2JavaTypeMapper.TypePrecedence.TYPE_ID);
        return messageConverter;
    }


    @Bean("rabbitListenerContainerFactory")
    public RabbitListenerContainerFactory<?> rabbitFactory(
            ConnectionFactory connectionFactory,
            MessageConverter converter
    ) {
        log.info("Building a default rabbit listener container factory");
        var factory = new SimpleRabbitListenerContainerFactory();
        factory.setConnectionFactory(connectionFactory);
        factory.setMessageConverter(converter);
        return factory;
    }

    @Bean("prefetchRabbitListenerContainerFactory")
    public RabbitListenerContainerFactory<SimpleMessageListenerContainer> prefetchRabbitListenerContainerFactory(
            ConnectionFactory rabbitConnectionFactory,
            MessageConverter converter
    ) {
        MDC.put("prefetch", String.valueOf(prefetch));
        log.info("Building a prefetch rabbit listener container factory with manual ack", prefetch);
        MDC.clear();
        var factory = new SimpleRabbitListenerContainerFactory();
        factory.setConnectionFactory(rabbitConnectionFactory);
        factory.setMessageConverter(converter);
        factory.setPrefetchCount(prefetch);
        factory.setAcknowledgeMode(AcknowledgeMode.MANUAL);
        return factory;
    }

    public AmqpTemplate rabbitTemplate(
            ConnectionFactory connectionFactory,
            MessageConverter converter
    ) {
        log.info("Building an amqp template");
        var rabbitTemplate = new RabbitTemplate(connectionFactory);
        rabbitTemplate.setMessageConverter(converter);
        return rabbitTemplate;
    }
}
