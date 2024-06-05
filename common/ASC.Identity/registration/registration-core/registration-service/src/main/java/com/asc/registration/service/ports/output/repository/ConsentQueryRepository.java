package com.asc.registration.service.ports.output.repository;

import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.service.transfer.response.PageableResponse;

/**
 * ConsentQueryRepository defines the contract for consent-related query operations. This repository
 * handles retrieving consents based on various query parameters.
 */
public interface ConsentQueryRepository {
  /**
   * Finds all consents belonging to a specific tenant and principal (user), with pagination
   * support.
   *
   * @param tenant The tenant ID to which the consents belong.
   * @param principalName The name of the principal (user) whose consents are to be retrieved.
   * @param page The page number to retrieve.
   * @param limit The number of consents per page.
   * @return A pageable response containing the consents for the specified tenant and principal.
   */
  PageableResponse<ClientConsent> findAllByTenantAndPrincipalName(
      TenantId tenant, String principalName, int page, int limit);
}
