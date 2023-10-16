/**
 *
 */
package com.onlyoffice.authorization.security.oauth.services;

import com.onlyoffice.authorization.core.entities.Consent;
import com.onlyoffice.authorization.core.transfer.messaging.ConsentMessage;
import com.onlyoffice.authorization.core.usecases.repositories.ConsentPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.external.messaging.configuration.RabbitMQConfiguration;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.cache.CacheManager;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsentService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.Assert;

import java.sql.Timestamp;
import java.time.Instant;
import java.util.Arrays;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class DocspaceOAuth2AuthorizationConsentService implements OAuth2AuthorizationConsentService,
        ConsentRetrieveUsecases {
    private final RabbitMQConfiguration configuration;
    private final ConsentPersistenceQueryUsecases consentUsecases;

    private final CacheManager cacheManager;
    private final AmqpTemplate amqpTemplate;

    @RateLimiter(name = "mutateRateLimiter")
    public void save(OAuth2AuthorizationConsent authorizationConsent) {
        Assert.notNull(authorizationConsent, "authorization consent cannot be null");
        MDC.put("client_id", authorizationConsent.getRegisteredClientId());
        MDC.put("principal_name", authorizationConsent.getPrincipalName());
        log.info("trying to save consent");
        MDC.clear();
        cacheManager.getCache("consent").put(String
                .format("%s:%s", authorizationConsent.getRegisteredClientId(),
                        authorizationConsent.getPrincipalName()), authorizationConsent);
        this.amqpTemplate.convertAndSend(
                configuration.getConsent().getExchange(),
                configuration.getConsent().getRouting(),
                toMessage(authorizationConsent)
        );
    }

    @RateLimiter(name = "mutateRateLimiter")
    public void remove(OAuth2AuthorizationConsent authorizationConsent) {
        Assert.notNull(authorizationConsent, "authorization consent cannot be null");
        MDC.put("client_id", authorizationConsent.getRegisteredClientId());
        MDC.put("principal_name", authorizationConsent.getPrincipalName());
        log.info("trying to remove consent");
        MDC.clear();
        var msg = toMessage(authorizationConsent);
        msg.setInvalidated(true);
        cacheManager.getCache("consent").evict(String
                .format("%s:%s", authorizationConsent.getRegisteredClientId(),
                        authorizationConsent.getPrincipalName()));
        this.amqpTemplate.convertAndSend(
                configuration.getConsent().getExchange(),
                configuration.getConsent().getRouting(),
                msg
        );
    }

    @RateLimiter(name = "getRateLimiter", fallbackMethod = "findConsentFallback")
    public OAuth2AuthorizationConsent findById(String registeredClientId, String principalName) {
        Assert.hasText(registeredClientId, "registered client id cannot be empty");
        Assert.hasText(principalName, "principal name cannot be empty");
        var cached = cacheManager.getCache("consent").get(String
                .format("%s:%s", registeredClientId, principalName));
        if (cached != null && (cached.get() instanceof OAuth2AuthorizationConsent consent)) {
            MDC.put("registered_client_id", registeredClientId);
            MDC.put("principal_name", principalName);
            log.info("found consent in-memory");
            MDC.clear();
            return consent;
        }

        MDC.put("client_id", registeredClientId);
        MDC.put("principal_name", principalName);
        log.info("trying to find consent with client_id and principal name", registeredClientId, principalName);
        MDC.clear();
        return toObject(this.consentUsecases.getByRegisteredClientIdAndPrincipalName(
                registeredClientId, principalName));
    }

    private OAuth2AuthorizationConsent findConsentFallback(String registeredClientId, String principalName, Throwable e) {
        MDC.put("client_id", registeredClientId);
        MDC.put("principal_name", principalName);
        log.warn("request is blocked due to rate-limiting for client id {} with principal name {}. Reason: {}",
                registeredClientId, principalName, e.getMessage());
        MDC.clear();
        return null;
    }

    private OAuth2AuthorizationConsent toObject(Consent consent) {
        String registeredClientId = consent.getRegisteredClientId();
        OAuth2AuthorizationConsent.Builder builder = OAuth2AuthorizationConsent.withId(
                registeredClientId, consent.getPrincipalName());
        Arrays.stream(consent.getScopes().split(",")).forEach(s -> builder.scope(s));
        return builder.build();
    }

    private ConsentMessage toMessage(OAuth2AuthorizationConsent authorizationConsent) {
        return ConsentMessage
                .builder()
                .registeredClientId(authorizationConsent.getRegisteredClientId())
                .principalName(authorizationConsent.getPrincipalName())
                .scopes(String.join(",", authorizationConsent.getScopes()))
                .modifiedAt(Timestamp.from(Instant.now()))
                .build();
    }
}
