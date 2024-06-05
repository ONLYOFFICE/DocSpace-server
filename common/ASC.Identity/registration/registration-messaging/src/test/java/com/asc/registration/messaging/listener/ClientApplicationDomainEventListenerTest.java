package com.asc.registration.messaging.listener;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.messaging.mapper.AuditDataMapper;
import com.asc.registration.service.ports.output.message.publisher.ClientAuditMessagePublisher;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class ClientApplicationDomainEventListenerTest {
  @InjectMocks private ClientApplicationDomainEventListener listener;
  @Mock private ClientAuditMessagePublisher messagePublisher;
  @Mock private AuditDataMapper auditDataMapper;
  @Mock private Audit audit;
  @Mock private ClientEvent clientEvent;
  @Mock private AuditMessage auditMessage;

  @BeforeEach
  void setUp() {
    when(clientEvent.getAudit()).thenReturn(audit);
    when(auditDataMapper.toMessage(audit)).thenReturn(auditMessage);
  }

  @Test
  void testProcess() {
    listener.process(clientEvent);

    verify(auditDataMapper).toMessage(audit);
    verify(messagePublisher).publish(auditMessage);
  }
}
