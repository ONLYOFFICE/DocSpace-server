package com.asc.registration.messaging.publisher;

import static org.mockito.Mockito.*;

import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.messaging.configuration.RabbitMQConfiguration;
import com.asc.registration.messaging.configuration.RabbitMQGenericQueueConfiguration;
import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.amqp.core.AmqpTemplate;

@ExtendWith(MockitoExtension.class)
public class RabbitClientAuditMessagePublisherTest {

  @InjectMocks private RabbitClientAuditMessagePublisher publisher;

  @Mock private RabbitMQConfiguration configuration;

  @Mock private AmqpTemplate amqpClient;

  @Mock private AuditMessage auditMessage;

  private RabbitMQGenericQueueConfiguration queueConfig;

  @BeforeEach
  void setUp() {
    queueConfig = new RabbitMQGenericQueueConfiguration();
    queueConfig.setExchange("exchange");
    queueConfig.setRouting("routing");

    when(configuration.getQueues()).thenReturn(Map.of("audit", queueConfig));
  }

  @Test
  void testPublish() {
    publisher.publish(auditMessage);
    verify(amqpClient).convertAndSend("exchange", "routing", auditMessage);
  }

  @Test
  void testPublishException() {
    doThrow(new RuntimeException())
        .when(amqpClient)
        .convertAndSend(anyString(), anyString(), (Object) any());
    publisher.publish(auditMessage);
  }
}
