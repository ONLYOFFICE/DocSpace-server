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
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 *
 */
@Getter
@Setter
@RequiredArgsConstructor
@Configuration
@ConfigurationProperties(prefix = "messaging.consent")
@Slf4j
public class ConsentQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue consentDeadQueue() {
        MDC.put("dead queue", configuration
                .getConsent().getDeadQueue());
        MDC.put("bytes limit", String.valueOf(configuration.getConsent()
                .getDeadMaxBytes()));
        log.info("Building a consent dead queue with bytes limit");
        MDC.clear();
        return QueueBuilder.durable(configuration.getConsent().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getConsent().getDeadMaxBytes())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue consentQueue() {
        MDC.put("queue", configuration.getConsent().getQueue());
        MDC.put("dead exchange", configuration.getConsent().getDeadExchange());
        MDC.put("max bytes", String.valueOf(configuration.getConsent().getMaxBytes()));
        MDC.put("delivery limit", String.valueOf(configuration.getConsent().getDeliveryLimit()));
        MDC.put("ttl", String.valueOf(configuration.getConsent().getMessageTTL()));
        log.info("Building a consent queue with dead exchange, max bytes, delivery limit and ttl");
        MDC.clear();
        return QueueBuilder.durable(configuration.getConsent().getQueue())
                .withArgument("x-dead-letter-exchange", configuration.getConsent().getDeadExchange())
                .withArgument("x-dead-letter-routing-key", configuration.getConsent().getDeadRouting())
                .withArgument("x-delivery-limit", configuration.getConsent().getDeliveryLimit())
                .withArgument("x-max-length-bytes", configuration.getConsent().getMaxBytes())
                .withArgument("x-message-ttl", configuration.getConsent().getMessageTTL())
                .withArgument("x-overflow", "reject-publish")
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public TopicExchange consentExchange() {
        MDC.put("exchange", configuration.getConsent().getExchange());
        log.info("Building a consent exchange");
        MDC.clear();
        return new TopicExchange(configuration.getConsent().getExchange());
    }

    @Bean
    public TopicExchange consentDeadExchange() {
        MDC.put("dead exchange", configuration.getConsent().getDeadExchange());
        log.info("Building a consent dead exchange");
        MDC.clear();
        return new TopicExchange(configuration.getConsent().getDeadExchange());
    }

    @Bean
    public Binding consentBinding() {
        MDC.put("binding", configuration.getConsent().getRouting());
        log.info("Building a consent binding");
        MDC.clear();
        return BindingBuilder.bind(consentQueue())
                .to(consentExchange())
                .with(configuration.getConsent().getRouting());
    }

    @Bean
    public Binding consentDeadBinding() {
        MDC.put("dead binding", configuration.getConsent().getDeadRouting());
        log.info("Building a consent dead binding");
        MDC.clear();
        return BindingBuilder.bind(consentDeadQueue())
                .to(consentDeadExchange())
                .with(configuration.getConsent().getDeadRouting());
    }
}
