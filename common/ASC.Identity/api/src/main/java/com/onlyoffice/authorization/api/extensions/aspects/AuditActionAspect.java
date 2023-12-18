package com.onlyoffice.authorization.api.extensions.aspects;

import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.extensions.annotations.AuditAction;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.server.messaging.messages.AuditMessage;
import com.onlyoffice.authorization.api.web.server.utilities.HttpUtils;
import lombok.RequiredArgsConstructor;
import org.aspectj.lang.JoinPoint;
import org.aspectj.lang.annotation.AfterReturning;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.reflect.MethodSignature;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.sql.Timestamp;
import java.time.Instant;

@Aspect
@Component
@RequiredArgsConstructor
public class AuditActionAspect {
    private final RabbitMQConfiguration configuration;
    private final AmqpTemplate amqpClient;

    @AfterReturning("@annotation(com.onlyoffice.authorization.api.extensions.annotations.AuditAction)")
    public void afterReturningAdvice(JoinPoint joinPoint) {
        var signature = (MethodSignature) joinPoint.getSignature();
        var method = signature.getMethod();
        var annotation = method.getAnnotation(AuditAction.class);

        var request = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes()).getRequest();
        var tenant = TenantContextContainer.context.get().getResponse();
        var person = PersonContextContainer.context.get().getResponse();

        amqpClient.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenant.getTenantId())
                        .userId(person.getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(annotation.action())
                        .build());
    }
}
