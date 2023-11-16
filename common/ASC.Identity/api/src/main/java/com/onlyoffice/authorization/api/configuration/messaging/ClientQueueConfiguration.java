/**
 *
 */
package com.onlyoffice.authorization.api.configuration.messaging;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.*;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@RequiredArgsConstructor
@Configuration
@Slf4j
public class ClientQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue clientDeadQueue() {
        MDC.put("dead queue", configuration
                .getClient().getDeadQueue());
        MDC.put("bytes limit", String.valueOf(configuration.getClient()
                .getDeadMaxBytes()));
        log.info("Building a client dead queue with bytes limit");
        MDC.clear();
        return QueueBuilder.durable(configuration.getClient().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getClient().getDeadMaxBytes())
                .withArgument("x-message-ttl", configuration.getClient().getMessageTTL())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue clientQueue() {
        MDC.put("queue", configuration.getClient().getQueue());
        MDC.put("dead exchange", configuration.getClient().getDeadExchange());
        MDC.put("max bytes", String.valueOf(configuration.getClient().getMaxBytes()));
        MDC.put("delivery limit", String.valueOf(configuration.getClient().getDeliveryLimit()));
        MDC.put("ttl", String.valueOf(configuration.getClient().getMessageTTL()));
        log.info("Building a client queue with dead exchange, max bytes delivery limit and ttl");
        MDC.clear();
        return QueueBuilder.durable(configuration.getClient().getQueue())
                .withArgument("x-dead-letter-exchange", configuration.getClient().getDeadExchange())
                .withArgument("x-dead-letter-routing-key", configuration.getClient().getDeadRouting())
                .withArgument("x-delivery-limit", configuration.getClient().getDeliveryLimit())
                .withArgument("x-max-length-bytes", configuration.getClient().getMaxBytes())
                .withArgument("x-message-ttl", configuration.getClient().getMessageTTL())
                .withArgument("x-overflow", "reject-publish")
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public TopicExchange clientExchange() {
        MDC.put("exchange", configuration.getClient().getExchange());
        log.info("Building a client exchange");
        MDC.clear();
        return new TopicExchange(configuration.getClient().getExchange());
    }

    @Bean
    public TopicExchange clientDeadExchange() {
        MDC.put("dead exchange", configuration.getClient().getDeadExchange());
        log.info("Building a client dead exchange");
        MDC.clear();
        return new TopicExchange(configuration.getClient().getDeadExchange());
    }

    @Bean
    public Binding clientBinding() {
        MDC.put("binding", configuration.getClient().getRouting());
        log.info("Building a client binding");
        MDC.clear();
        return BindingBuilder.bind(clientQueue())
                .to(clientExchange())
                .with(configuration.getClient().getRouting());
    }

    @Bean
    public Binding clientDeadBinding() {
        MDC.put("binding", configuration.getClient().getDeadRouting());
        log.info("Building a client dead binding");
        MDC.clear();
        return BindingBuilder.bind(clientDeadQueue())
                .to(clientDeadExchange())
                .with(configuration.getClient().getDeadRouting());
    }
}
