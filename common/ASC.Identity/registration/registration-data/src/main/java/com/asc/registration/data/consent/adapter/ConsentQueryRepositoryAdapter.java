// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.data.consent.adapter;

import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import com.asc.registration.core.domain.entity.ClientConsent;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.consent.mapper.ConsentDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.LinkedHashSet;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
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
            principalId, PageRequest.of(page, limit, Sort.by("modifiedAt").descending()));

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
                    .collect(Collectors.toCollection(LinkedHashSet::new)));

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
            principalId,
            tenantId.getValue(),
            PageRequest.of(page, limit, Sort.by("modifiedAt").descending()));

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
                    .collect(Collectors.toCollection(LinkedHashSet::new)));

    if (consents.hasPrevious()) builder.previous(page - 1);

    if (consents.hasNext()) builder.next(page + 1);

    return builder.build();
  }
}
