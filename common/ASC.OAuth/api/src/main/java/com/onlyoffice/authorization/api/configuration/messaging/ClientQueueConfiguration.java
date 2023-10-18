/**
 *
 */
package com.onlyoffice.authorization.api.configuration.messaging;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
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
        log.info("Building a client dead queue {} with bytes limit of {}", configuration
                .getClient().getDeadQueue(), configuration.getClient().getDeadMaxBytes());
        return QueueBuilder.durable(configuration.getClient().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getClient().getDeadMaxBytes())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue clientQueue() {
        log.info("Building a client queue {} with dead exchange {}, max bytes {} delivery limit {} and ttl {}",
                configuration.getClient().getQueue(), configuration.getClient().getDeadExchange(),
                configuration.getClient().getMaxBytes(), configuration.getClient().getDeliveryLimit(),
                configuration.getClient().getMessageTTL());
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
        log.info("Building a client exchange {}", configuration.getClient().getExchange());
        return new TopicExchange(configuration.getClient().getExchange());
    }

    @Bean
    public TopicExchange clientDeadExchange() {
        log.info("Building a client dead exchange {}", configuration.getClient().getDeadExchange());
        return new TopicExchange(configuration.getClient().getDeadExchange());
    }

    @Bean
    public Binding clientBinding() {
        log.info("Building a client binding with {}", configuration.getClient().getRouting());
        return BindingBuilder.bind(clientQueue())
                .to(clientExchange())
                .with(configuration.getClient().getRouting());
    }

    @Bean
    public Binding clientDeadBinding() {
        log.info("Building a client dead binding with {}", configuration.getClient().getDeadRouting());
        return BindingBuilder.bind(clientDeadQueue())
                .to(clientDeadExchange())
                .with(configuration.getClient().getDeadRouting());
    }
}
