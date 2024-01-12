/**
 *
 */
package com.onlyoffice.authorization.web.security.oauth.services;

import com.onlyoffice.authorization.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.core.entities.Consent;
import com.onlyoffice.authorization.core.usecases.repositories.ConsentPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.web.server.messaging.ConsentMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.cache.CacheManager;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsentService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.ZonedDateTime;
import java.util.Arrays;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class AscOAuth2AuthorizationConsentService implements OAuth2AuthorizationConsentService,
        ConsentRetrieveUsecases {
    private final String CONSENT_QUEUE = "consent";

    private final RabbitMQConfiguration configuration;

    private final ConsentPersistenceQueryUsecases consentUsecases;

    private final CacheManager cacheManager;
    private final AmqpTemplate amqpTemplate;

    /**
     *
     * @param authorizationConsent the {@link OAuth2AuthorizationConsent}
     */
    public void save(OAuth2AuthorizationConsent authorizationConsent) {
        MDC.put("clientId", authorizationConsent.getRegisteredClientId());
        MDC.put("principalName", authorizationConsent.getPrincipalName());

        log.info("Publishing an authorization save consent message");

        try {
            CompletableFuture.allOf(
                    CompletableFuture.runAsync(() -> amqpTemplate.convertAndSend(
                            configuration.getQueues().get(CONSENT_QUEUE).getExchange(),
                            configuration.getQueues().get(CONSENT_QUEUE).getRouting(),
                            toMessage(authorizationConsent))
                    ),
                    CompletableFuture.runAsync(() -> cacheManager.getCache(CONSENT_QUEUE)
                            .put(String.format("%s:%s", authorizationConsent.getRegisteredClientId(),
                                    authorizationConsent.getPrincipalName()),
                                    authorizationConsent))
                    )
                    .get(2, TimeUnit.SECONDS);
        } catch (Exception e) {
            log.error("Could not save an oauth2 authorization consent", e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param authorizationConsent the {@link OAuth2AuthorizationConsent}
     */
    public void remove(OAuth2AuthorizationConsent authorizationConsent) {
        MDC.put("clientId", authorizationConsent.getRegisteredClientId());
        MDC.put("principalName", authorizationConsent.getPrincipalName());

        var msg = toMessage(authorizationConsent);
        msg.setInvalidated(true);

        log.info("Submitting an authorization consent delete message");

        try {
            CompletableFuture.allOf(
                            CompletableFuture.runAsync(() -> amqpTemplate.convertAndSend(
                                    configuration.getQueues().get(CONSENT_QUEUE).getExchange(),
                                    configuration.getQueues().get(CONSENT_QUEUE).getRouting(),
                                    msg)
                            ),
                            CompletableFuture.runAsync(() -> cacheManager
                                    .getCache(CONSENT_QUEUE).evictIfPresent(String
                                            .format("%s:%s", authorizationConsent.getRegisteredClientId(),
                                            authorizationConsent.getPrincipalName())))
                    )
                    .get(2, TimeUnit.SECONDS);
        } catch (Exception e) {
            log.error("Could not remove an oauth2 authorization consent", e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param registeredClientId the identifier for the {@link RegisteredClient}
     * @param principalName the name of the {@link Principal}
     * @return
     */
    public OAuth2AuthorizationConsent findById(String registeredClientId, String principalName) {
        MDC.put("registeredClientId", registeredClientId);
        MDC.put("principalName", principalName);
        log.info("Trying to find an authorization consent in the cache");

        var cached = cacheManager.getCache(CONSENT_QUEUE).get(String
                .format("%s:%s", registeredClientId, principalName));

        if (cached != null && (cached.get() instanceof OAuth2AuthorizationConsent consent)) {
            log.info("Found authorization consent consent in the cache");
            MDC.clear();

            cacheManager.getCache(CONSENT_QUEUE).evict(String
                    .format("%s:%s", registeredClientId, principalName));
            return consent;
        }

        log.info("Trying to find authorization consent in the database");
        MDC.clear();

        var consent = consentUsecases.getByRegisteredClientIdAndPrincipalName(
                registeredClientId, principalName);
        if (consent == null)
            return null;

        cacheManager.getCache(CONSENT_QUEUE).putIfAbsent(String
                .format("%s:%s", registeredClientId, principalName), consent);

        return toObject(consent);
    }

    /**
     *
     * @param consent
     * @return
     */
    private OAuth2AuthorizationConsent toObject(Consent consent) {
        String registeredClientId = consent.getRegisteredClientId();
        OAuth2AuthorizationConsent.Builder builder = OAuth2AuthorizationConsent.withId(
                registeredClientId, consent.getPrincipalName());
        Arrays.stream(consent.getScopes().split(",")).forEach(s -> builder.scope(s));
        return builder.build();
    }

    /**
     *
     * @param authorizationConsent
     * @return
     */
    private ConsentMessage toMessage(OAuth2AuthorizationConsent authorizationConsent) {
        return ConsentMessage
                .builder()
                .registeredClientId(authorizationConsent.getRegisteredClientId())
                .principalName(authorizationConsent.getPrincipalName())
                .scopes(String.join(",", authorizationConsent.getScopes()))
                .modifiedAt(ZonedDateTime.now())
                .build();
    }
}
