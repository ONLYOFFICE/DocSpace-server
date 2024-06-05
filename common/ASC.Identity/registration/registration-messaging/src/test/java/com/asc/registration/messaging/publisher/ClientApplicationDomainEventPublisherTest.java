package com.asc.registration.messaging.publisher;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.registration.core.domain.event.ClientEvent;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.context.ApplicationEventPublisher;

@ExtendWith(MockitoExtension.class)
public class ClientApplicationDomainEventPublisherTest {
  @InjectMocks private ClientApplicationDomainEventPublisher publisher;
  @Mock private ApplicationEventPublisher applicationEventPublisher;
  @Mock private Audit audit;
  @Mock private ClientEvent clientEvent;

  @BeforeEach
  void setUp() {
    when(clientEvent.getAudit()).thenReturn(audit);
    when(audit.getUserId()).thenReturn("testUser");
    when(audit.getAuditCode()).thenReturn(AuditCode.CREATE_CLIENT);
  }

  @Test
  void testPublish() {
    publisher.publish(clientEvent);
    verify(applicationEventPublisher).publishEvent(clientEvent);
  }
}
