/**
 *
 */
package com.onlyoffice.authorization.api.configuration.messaging;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
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
        log.info("Building a consent dead queue {} with bytes limit of {}", configuration
                .getConsent().getDeadQueue(), configuration.getConsent().getDeadMaxBytes());
        return QueueBuilder.durable(configuration.getConsent().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getConsent().getDeadMaxBytes())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue consentQueue() {
        log.info("Building a consent queue {} with dead exchange {}, max bytes {} delivery limit {} and ttl {}",
                configuration.getConsent().getQueue(), configuration.getConsent().getDeadExchange(),
                configuration.getConsent().getMaxBytes(), configuration.getConsent().getDeliveryLimit(),
                configuration.getConsent().getMessageTTL());
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
        log.info("Building a consent exchange {}", configuration.getConsent().getExchange());
        return new TopicExchange(configuration.getConsent().getExchange());
    }

    @Bean
    public TopicExchange consentDeadExchange() {
        log.info("Building a consent dead exchange {}", configuration.getConsent().getDeadExchange());
        return new TopicExchange(configuration.getConsent().getDeadExchange());
    }

    @Bean
    public Binding consentBinding() {
        log.info("Building a consent binding with {}", configuration.getConsent().getRouting());
        return BindingBuilder.bind(consentQueue())
                .to(consentExchange())
                .with(configuration.getConsent().getRouting());
    }

    @Bean
    public Binding consentDeadBinding() {
        log.info("Building a consent dead binding with {}", configuration.getConsent().getDeadRouting());
        return BindingBuilder.bind(consentDeadQueue())
                .to(consentDeadExchange())
                .with(configuration.getConsent().getDeadRouting());
    }
}
