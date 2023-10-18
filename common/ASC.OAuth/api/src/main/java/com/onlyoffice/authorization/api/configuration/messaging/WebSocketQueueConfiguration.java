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
@ConfigurationProperties(prefix = "messaging.socket")
@Slf4j
public class WebSocketQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue socketQueue() {
        return QueueBuilder.nonDurable(configuration.getSocket().getQueue())
                .autoDelete()
                .withArgument("x-max-length-bytes", configuration.getSocket().getMaxBytes())
                .withArgument("x-message-ttl", configuration.getSocket().getMessageTTL())
                .withArgument("x-overflow", "reject-publish")
                .build();
    }

    @Bean
    public FanoutExchange socketExchange() {
        log.info("Building a socket exchange {}", configuration.getSocket().getExchange());
        return new FanoutExchange(configuration.getSocket().getExchange());
    }

    @Bean
    public Binding socketBinding() {
        log.info("Building a consent binding with {}", configuration.getSocket().getRouting());
        return BindingBuilder.bind(socketQueue())
                .to(socketExchange());
    }
}
