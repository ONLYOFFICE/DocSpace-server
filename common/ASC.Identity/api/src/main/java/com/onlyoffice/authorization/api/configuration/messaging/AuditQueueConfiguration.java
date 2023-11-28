package com.onlyoffice.authorization.api.configuration.messaging;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.*;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Getter
@Setter
@RequiredArgsConstructor
@Configuration
@Slf4j
public class AuditQueueConfiguration {
    private final RabbitMQConfiguration configuration;

    @Bean
    public Queue auditDeadQueue() {
        MDC.put("dead queue", configuration.getAudit().getDeadQueue());
        MDC.put("bytes limit", String.valueOf(configuration.getAudit().getDeadMaxBytes()));
        log.info("Building an audit dead queue with bytes limit");
        MDC.clear();
        return QueueBuilder.durable(configuration.getAudit().getDeadQueue())
                .withArgument("x-max-length-bytes", configuration.getAudit().getDeadMaxBytes())
                .withArgument("x-message-ttl", configuration.getAudit().getMessageTTL())
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public Queue auditQueue() {
        MDC.put("queue", configuration.getAudit().getQueue());
        MDC.put("dead exchange", configuration.getAudit().getDeadExchange());
        MDC.put("max bytes", String.valueOf(configuration.getAudit().getMaxBytes()));
        MDC.put("delivery limit", String.valueOf(configuration.getAudit().getDeliveryLimit()));
        MDC.put("ttl", String.valueOf(configuration.getAudit().getMessageTTL()));
        log.info("Building an audit queue with dead exchange, max bytes delivery limit and ttl");
        MDC.clear();
        return QueueBuilder.durable(configuration.getAudit().getQueue())
                .withArgument("x-dead-letter-exchange", configuration.getAudit().getDeadExchange())
                .withArgument("x-dead-letter-routing-key", configuration.getAudit().getDeadRouting())
                .withArgument("x-delivery-limit", configuration.getAudit().getDeliveryLimit())
                .withArgument("x-max-length-bytes", configuration.getAudit().getMaxBytes())
                .withArgument("x-message-ttl", configuration.getAudit().getMessageTTL())
                .withArgument("x-overflow", "reject-publish")
                .withArgument("x-queue-type", "quorum")
                .build();
    }

    @Bean
    public TopicExchange auditExchange() {
        MDC.put("exchange", configuration.getAudit().getExchange());
        log.info("Building an audit exchange");
        MDC.clear();
        return new TopicExchange(configuration.getAudit().getExchange());
    }

    @Bean
    public TopicExchange auditDeadExchange() {
        MDC.put("dead exchange", configuration.getAudit().getDeadExchange());
        log.info("Building an audit dead exchange");
        MDC.clear();
        return new TopicExchange(configuration.getAudit().getDeadExchange());
    }

    @Bean
    public Binding auditBinding() {
        MDC.put("binding", configuration.getAudit().getRouting());
        log.info("Building an audit binding");
        MDC.clear();
        return BindingBuilder.bind(auditQueue())
                .to(auditExchange())
                .with(configuration.getAudit().getRouting());
    }

    @Bean
    public Binding auditDeadBinding() {
        MDC.put("dead binding", configuration.getAudit().getDeadRouting());
        log.info("Building an audit dead binding");
        MDC.clear();
        return BindingBuilder.bind(auditDeadQueue())
                .to(auditDeadExchange())
                .with(configuration.getAudit().getDeadRouting());
    }
}
