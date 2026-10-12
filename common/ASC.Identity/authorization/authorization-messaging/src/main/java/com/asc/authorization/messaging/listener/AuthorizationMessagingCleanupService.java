package com.asc.authorization.messaging.listener;

import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
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
  private final int AUTHORIZATION_CHUNK_SIZE = 1000;
  private final int CONSENT_CHUNK_SIZE = 500;
  private final int TIMEOUT_SECONDS = 5;

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

  public void onClientRemoved(String clientId) {
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByClientId(clientId, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByClientId(clientId, limit),
        CONSENT_CHUNK_SIZE);
  }

  public void onUserRemoved(long tenantId, String userId) {
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByPrincipalId(userId, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByPrincipalId(userId, limit),
        CONSENT_CHUNK_SIZE);

    if (tenantId <= 0) {
      log.warn(
          "Skipping removal of grants made to the clients of user {}: tenant id is unknown",
          userId);
      return;
    }

    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByOwner(tenantId, userId, limit),
        AUTHORIZATION_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByOwner(tenantId, userId, limit),
        CONSENT_CHUNK_SIZE);
  }

  public void onTenantRemoved(long tenantId) {
    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByTenantId(tenantId, limit),
        CONSENT_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByTenantId(tenantId, limit),
        AUTHORIZATION_CHUNK_SIZE);

    handleChunkedDelete(
        limit -> jpaConsentRepository.deleteConsentsByOwnerTenantId(tenantId, limit),
        CONSENT_CHUNK_SIZE);
    handleChunkedDelete(
        limit -> jpaAuthorizationRepository.deleteAuthorizationsByOwnerTenantId(tenantId, limit),
        AUTHORIZATION_CHUNK_SIZE);
  }
}
