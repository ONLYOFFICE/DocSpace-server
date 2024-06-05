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
   * Finds all user consents by tenant ID and principal name.
   *
   * @param tenantId the tenant ID
   * @param principalName the principal (user) name
   * @param page the page number
   * @param limit the page size limit
   * @return a pageable response containing the user consents
   */
  public PageableResponse<ClientConsent> findAllByTenantAndPrincipalName(
      TenantId tenantId, String principalName, int page, int limit) {

    log.debug("Querying all user's consent by tenant id");

    var consents =
        jpaConsentRepository.findAllConsentsByPrincipalNameAndTenant(
            principalName, tenantId.getValue(), Pageable.ofSize(limit).withPage(page));

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

    if (consents.hasPrevious()) {
      builder.previous(page - 1);
    }

    if (consents.hasNext()) {
      builder.next(page + 1);
    }

    return builder.build();
  }
}
