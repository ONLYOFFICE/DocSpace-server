package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.ClientId;

/**
 * ConsentCommandRepository defines the contract for consent-related operations that modify the
 * state of consents. This repository handles revoking consents for clients.
 */
public interface ConsentCommandRepository {
  /**
   * Revokes the consent of a specific client for a given principal (user).
   *
   * @param clientId The unique client ID whose consent is to be revoked.
   * @param principalName The name of the principal (user) whose consent is to be revoked.
   */
  void revokeConsent(ClientId clientId, String principalName);
}
