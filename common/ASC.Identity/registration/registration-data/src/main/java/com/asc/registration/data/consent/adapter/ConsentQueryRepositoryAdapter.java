package com.asc.registration.data.consent.adapter;

import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.consent.mapper.ConsentDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling consent query operations. Implements the {@link
 * ConsentQueryRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ConsentQueryRepositoryAdapter implements ConsentQueryRepository {
  private final JpaConsentRepository jpaConsentRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;
  private final ConsentDataAccessMapper consentDataAccessMapper;

  /**
   * Retrieves all consents for a specific principal (user) with pagination support.
   *
   * @param principalId the ID of the principal (user)
   * @param page the page number to retrieve
   * @param limit the maximum number of items per page
   * @return a pageable response containing the consents
   */
  public PageableResponse<ClientConsent> findAllByPrincipalId(
      String principalId, int page, int limit) {
    log.debug("Querying all user's consent by principal id");

    var consents =
        jpaConsentRepository.findAllConsentsByPrincipalId(
            principalId, Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<ClientConsent>builder()
            .page(page)
            .limit(limit)
            .data(
                consents.stream()
                    .map(
                        c ->
                            consentDataAccessMapper.toClientConsent(
                                c, clientDataAccessMapper.toDomain(c.getClient())))
                    .collect(Collectors.toSet()));

    if (consents.hasPrevious()) builder.previous(page - 1);

    if (consents.hasNext()) builder.next(page + 1);

    return builder.build();
  }

  /**
   * Retrieves all consents for a specific tenant and principal (user) with pagination support.
   *
   * @param tenantId the tenant ID
   * @param principalId the principal (user) ID
   * @param page the page number to retrieve
   * @param limit the maximum number of items per page
   * @return a pageable response containing the consents
   */
  public PageableResponse<ClientConsent> findAllByTenantIdAndPrincipalId(
      TenantId tenantId, String principalId, int page, int limit) {
    log.debug("Querying all user's consent by tenant id");

    var consents =
        jpaConsentRepository.findAllConsentsByPrincipalIdAndTenant(
            principalId, tenantId.getValue(), Pageable.ofSize(limit).withPage(page));

    var builder =
        PageableResponse.<ClientConsent>builder()
            .page(page)
            .limit(limit)
            .data(
                consents.stream()
                    .map(
                        c ->
                            consentDataAccessMapper.toClientConsent(
                                c, clientDataAccessMapper.toDomain(c.getClient())))
                    .collect(Collectors.toSet()));

    if (consents.hasPrevious()) builder.previous(page - 1);

    if (consents.hasNext()) builder.next(page + 1);

    return builder.build();
  }
}
