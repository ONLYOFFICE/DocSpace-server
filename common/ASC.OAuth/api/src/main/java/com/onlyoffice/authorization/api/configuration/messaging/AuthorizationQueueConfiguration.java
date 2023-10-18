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
public class AuthorizationQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue authorizationDeadQueue() {
        log.info("Building an authorization dead queue {} with bytes limit of {}", configuration
                .getAuthorization().getDeadQueue(), configuration.getAuthorization().getDeadMaxBytes());
        return QueueBuilder.durable(configuration.getAuthorization().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getAuthorization().getDeadMaxBytes())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue authorizationQueue() {
        log.info("Building an authorization queue {} with dead exchange {}, max bytes {} delivery limit {} and ttl {}",
                configuration.getAuthorization().getQueue(), configuration.getAuthorization().getDeadExchange(),
                configuration.getAuthorization().getMaxBytes(), configuration.getAuthorization().getDeliveryLimit(),
                configuration.getAuthorization().getMessageTTL());
        return QueueBuilder.durable(configuration.getAuthorization().getQueue())
                .withArgument("x-dead-letter-exchange", configuration.getAuthorization().getDeadExchange())
                .withArgument("x-dead-letter-routing-key", configuration.getAuthorization().getDeadRouting())
                .withArgument("x-delivery-limit", configuration.getAuthorization().getDeliveryLimit())
                .withArgument("x-max-length-bytes", configuration.getAuthorization().getMaxBytes())
                .withArgument("x-message-ttl", configuration.getAuthorization().getMessageTTL())
                .withArgument("x-overflow", "reject-publish")
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public TopicExchange authorizationExchange() {
        log.info("Building an authorization exchange {}", configuration.getAuthorization().getExchange());
        return new TopicExchange(configuration.getAuthorization().getExchange());
    }

    @Bean
    public TopicExchange authorizationDeadExchange() {
        log.info("Building an authorization dead exchange {}", configuration.getAuthorization().getDeadExchange());
        return new TopicExchange(configuration.getAuthorization().getDeadExchange());
    }

    @Bean
    public Binding authorizationBinding() {
        log.info("Building an authorization binding with {}", configuration.getAuthorization().getRouting());
        return BindingBuilder.bind(authorizationQueue())
                .to(authorizationExchange())
                .with(configuration.getAuthorization().getRouting());
    }

    @Bean
    public Binding authorizationDeadBinding() {
        log.info("Building an authorization dead binding with {}", configuration.getAuthorization().getDeadRouting());
        return BindingBuilder.bind(authorizationDeadQueue())
                .to(authorizationDeadExchange())
                .with(configuration.getAuthorization().getDeadRouting());
    }
}
