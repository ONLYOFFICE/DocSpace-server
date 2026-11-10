package com.asc.authorization.messaging.listener;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import java.time.Instant;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.Objects;
import java.util.function.IntUnaryOperator;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.TransactionDefinition;
import org.springframework.transaction.support.TransactionTemplate;

@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationMessagingCleanupService {
  private static final int AUTHORIZATION_CHUNK_SIZE = 1000;
  private static final int CONSENT_CHUNK_SIZE = 500;
  private static final int TIMEOUT_SECONDS = 5;

  private final PlatformTransactionManager transactionManager;
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final JpaConsentRepository jpaConsentRepository;

  private void handleChunkedDelete(IntUnaryOperator chunk, int chunkSize) {
    var template = new TransactionTemplate(transactionManager);
    template.setIsolationLevel(TransactionDefinition.ISOLATION_READ_COMMITTED);
    template.setTimeout(TIMEOUT_SECONDS);

    long total = 0;
    int deleted;
    do {
      deleted = Objects.requireNonNullElse(template.execute(_ -> chunk.applyAsInt(chunkSize)), 0);
      total += deleted;
    } while (deleted >= chunkSize);

    if (total > 0) log.info("Chunked removal of {} items", total);
  }

  private static ZonedDateTime cutoffOf(Instant removedAt) {
    return (removedAt != null ? removedAt : Instant.now()).atZone(ZoneOffset.UTC);
  }

  public void onClientRemoved(String clientId) {
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByClientId(clientId, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByClientId(clientId, limit),
        CONSENT_CHUNK_SIZE);
  }

  public void onUserRemoved(long tenantId, String userId, Instant removedAt) {
    var cutoff = cutoffOf(removedAt);
    handleChunkedDelete(
        limit ->
            jpaAuthorizationRepository.deleteAuthorizationsByPrincipalId(userId, cutoff, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByPrincipalId(userId, cutoff, limit),
        CONSENT_CHUNK_SIZE);

    if (tenantId <= 0) {
      log.warn(
          "Skipping removal of grants made to the clients of user {}: tenant id is unknown",
          userId);
      return;
    }

    handleChunkedDelete(
        limit ->
            jpaAuthorizationRepository.deleteAuthorizationsByOwner(tenantId, userId, cutoff, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByOwner(tenantId, userId, cutoff, limit),
        CONSENT_CHUNK_SIZE);
  }

  public void onTenantRemoved(long tenantId, Instant removedAt) {
    var cutoff = cutoffOf(removedAt);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByTenantId(tenantId, cutoff, limit),
        CONSENT_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByTenantId(tenantId, cutoff, limit),
        AUTHORIZATION_CHUNK_SIZE);

    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByOwnerTenantId(tenantId, cutoff, limit),
        CONSENT_CHUNK_SIZE);
    handleChunkedDelete(
        limit ->
            jpaAuthorizationRepository.deleteAuthorizationsByOwnerTenantId(tenantId, cutoff, limit),
        AUTHORIZATION_CHUNK_SIZE);
  }
}
