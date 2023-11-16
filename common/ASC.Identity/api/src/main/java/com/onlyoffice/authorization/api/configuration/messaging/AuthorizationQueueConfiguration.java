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
public class AuthorizationQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue authorizationDeadQueue() {
        MDC.put("dead queue", configuration.getAuthorization().getDeadQueue());
        MDC.put("bytes limit", String.valueOf(configuration.getAuthorization().getDeadMaxBytes()));
        log.info("Building an authorization dead queue with bytes limit");
        MDC.clear();
        return QueueBuilder.durable(configuration.getAuthorization().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getAuthorization().getDeadMaxBytes())
                .withArgument("x-message-ttl", configuration.getAuthorization().getMessageTTL())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue authorizationQueue() {
        MDC.put("queue", configuration.getAuthorization().getQueue());
        MDC.put("dead exchange", configuration.getAuthorization().getDeadExchange());
        MDC.put("max bytes", String.valueOf(configuration.getAuthorization().getMaxBytes()));
        MDC.put("delivery limit", String.valueOf(configuration.getAuthorization().getDeliveryLimit()));
        MDC.put("ttl", String.valueOf(configuration.getAuthorization().getMessageTTL()));
        log.info("Building an authorization queue with dead exchange, max bytes delivery limit and ttl");
        MDC.clear();
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
        MDC.put("exchange", configuration.getAuthorization().getExchange());
        log.info("Building an authorization exchange");
        MDC.clear();
        return new TopicExchange(configuration.getAuthorization().getExchange());
    }

    @Bean
    public TopicExchange authorizationDeadExchange() {
        MDC.put("dead exchange", configuration.getAuthorization().getDeadExchange());
        log.info("Building an authorization dead exchange");
        MDC.clear();
        return new TopicExchange(configuration.getAuthorization().getDeadExchange());
    }

    @Bean
    public Binding authorizationBinding() {
        MDC.put("binding", configuration.getAuthorization().getRouting());
        log.info("Building an authorization binding");
        MDC.clear();
        return BindingBuilder.bind(authorizationQueue())
                .to(authorizationExchange())
                .with(configuration.getAuthorization().getRouting());
    }

    @Bean
    public Binding authorizationDeadBinding() {
        MDC.put("dead binding", configuration.getAuthorization().getDeadRouting());
        log.info("Building an authorization dead binding");
        MDC.clear();
        return BindingBuilder.bind(authorizationDeadQueue())
                .to(authorizationDeadExchange())
                .with(configuration.getAuthorization().getDeadRouting());
    }
}
