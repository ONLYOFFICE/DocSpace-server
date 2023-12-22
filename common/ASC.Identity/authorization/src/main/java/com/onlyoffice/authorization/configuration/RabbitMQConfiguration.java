/**
 *
 */
package com.onlyoffice.authorization.configuration;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.onlyoffice.authorization.web.server.messaging.AuthorizationMessage;
import com.onlyoffice.authorization.web.server.messaging.ConsentMessage;
import lombok.Getter;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AcknowledgeMode;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
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

    /**
     *
     * @param mapper
     * @return
     */
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

    /**
     *
     * @param connectionFactory
     * @param converter
     * @return
     */
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

    /**
     *
     * @param rabbitConnectionFactory
     * @param converter
     * @return
     */
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

    /**
     *
     * @param connectionFactory
     * @param converter
     * @return
     */
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
