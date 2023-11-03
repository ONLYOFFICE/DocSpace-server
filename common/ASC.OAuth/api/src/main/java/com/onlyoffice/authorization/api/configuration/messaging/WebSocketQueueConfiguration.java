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
        MDC.put("exchange", configuration.getSocket().getExchange());
        log.info("Building a socket exchange");
        MDC.clear();
        return new FanoutExchange(configuration.getSocket().getExchange());
    }

    @Bean
    public Binding socketBinding() {
        MDC.put("binding", configuration.getSocket().getRouting());
        log.info("Building a consent binding");
        MDC.clear();
        return BindingBuilder.bind(socketQueue())
                .to(socketExchange());
    }
}
