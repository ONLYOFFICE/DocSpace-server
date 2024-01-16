package com.asc.authorization.api.extensions.aspects;

import com.asc.authorization.api.configuration.RabbitMQConfiguration;
import com.asc.authorization.api.extensions.annotations.AuditAction;
import com.asc.authorization.api.web.security.context.PersonContextContainer;
import com.asc.authorization.api.web.security.context.TenantContextContainer;
import com.asc.authorization.api.web.server.messaging.messages.AuditMessage;
import com.asc.authorization.api.web.server.utilities.HttpUtils;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.aspectj.lang.JoinPoint;
import org.aspectj.lang.annotation.AfterReturning;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.reflect.MethodSignature;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.time.ZonedDateTime;

/**
 *
 */
@Slf4j
@Aspect
@Component
@RequiredArgsConstructor
public class AuditActionAspect {
    private final RabbitMQConfiguration configuration;
    private final AmqpTemplate amqpClient;

    /**
     *
     * @param joinPoint
     */
    @AfterReturning("@annotation(com.asc.authorization.api.extensions.annotations.AuditAction)")
    public void afterReturningAdvice(JoinPoint joinPoint) {
        var signature = (MethodSignature) joinPoint.getSignature();
        var method = signature.getMethod();
        var annotation = method.getAnnotation(AuditAction.class);

        var request = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes()).getRequest();
        var tenant = TenantContextContainer.context.get().getResponse();
        var person = PersonContextContainer.context.get().getResponse();

        var queue = configuration.getQueues().get("audit");
        var msg = AuditMessage.builder()
                .ip(HttpUtils.getFirstRequestIP(request))
                .browser(HttpUtils.getClientBrowser(request))
                .platform(HttpUtils.getClientOS(request))
                .date(ZonedDateTime.now())
                .tenantId(tenant.getTenantId())
                .userId(person.getId())
                .page(HttpUtils.getFullURL(request))
                .actionEnum(annotation.action())
                .build();

        log.debug("Sending an audit message", msg);

        amqpClient.convertAndSend(
                queue.getExchange(),
                queue.getRouting(),
                msg);
    }
}
