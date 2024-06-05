package com.asc.registration.messaging.listener;

import static org.mockito.ArgumentMatchers.anySet;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.messaging.mapper.AuditDataMapper;
import com.rabbitmq.client.Channel;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.messaging.Message;

@ExtendWith(MockitoExtension.class)
public class RabbitClientAuditMessageListenerTest {
  @InjectMocks private RabbitClientAuditMessageListener listener;
  @Mock private AuditCreateCommandHandler auditCreateCommandHandler;
  @Mock private AuditDataMapper auditDataMapper;
  @Mock private Audit audit;
  @Mock private AuditMessage auditMessage;
  @Mock private Message<AuditMessage> message;
  @Mock private Channel channel;

  @BeforeEach
  void setUp() {
    when(message.getPayload()).thenReturn(auditMessage);
    when(auditDataMapper.toAudit(auditMessage)).thenReturn(audit);
  }

  @Test
  void testReceiveMessage() {
    List<Message<AuditMessage>> messages = Stream.of(message).collect(Collectors.toList());
    listener.receiveMessage(messages, channel);
    verify(auditCreateCommandHandler).createAudits(anySet());
  }
}
