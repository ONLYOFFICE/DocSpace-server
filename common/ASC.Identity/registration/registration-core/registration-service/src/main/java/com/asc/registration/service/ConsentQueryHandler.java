package com.asc.registration.service;

import com.asc.common.core.domain.value.TenantId;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.mapper.ConsentDataMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.request.fetch.TenantConsentsPaginationQuery;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Slf4j
@Component
@RequiredArgsConstructor
public class ConsentQueryHandler {
  private final ConsentQueryRepository consentQueryRepository;
  private final ClientDataMapper clientDataMapper;
  private final ConsentDataMapper consentDataMapper;

  /**
   * Retrieves consents for a tenant and principal name with pagination.
   *
   * @param query the pagination query containing tenant ID, principal name, page, and limit
   * @return a pageable response containing the consents
   */
  @Transactional(timeout = 2)
  public PageableResponse<ConsentResponse> getConsents(TenantConsentsPaginationQuery query) {
    log.info("Trying to get user consents for current tenant");

    var result =
        consentQueryRepository.findAllByTenantAndPrincipalName(
            new TenantId(query.getTenantId()),
            query.getPrincipalName(),
            query.getPage(),
            query.getLimit());
    return PageableResponse.<ConsentResponse>builder()
        .page(result.getPage())
        .limit(result.getLimit())
        .data(
            StreamSupport.stream(result.getData().spliterator(), false)
                .map(
                    clientConsent ->
                        consentDataMapper.toConsentResponse(
                            clientConsent.getConsent(),
                            clientDataMapper.toClientResponse(clientConsent.getClient())))
                .collect(Collectors.toSet()))
        .next(result.getNext())
        .previous(result.getPrevious())
        .build();
  }
}
