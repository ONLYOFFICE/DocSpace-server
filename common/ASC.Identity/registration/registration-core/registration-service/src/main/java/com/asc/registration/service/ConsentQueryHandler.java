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

package com.asc.registration.service;

import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.mapper.ConsentDataMapper;
import com.asc.registration.service.ports.output.repository.ConsentQueryRepository;
import com.asc.registration.service.transfer.request.fetch.ConsentsPaginationQuery;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.LinkedHashSet;
import java.util.stream.Collectors;
import java.util.stream.StreamSupport;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * ConsentQueryHandler handles query operations related to client consents. It retrieves consent
 * information based on various query parameters.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ConsentQueryHandler {
  private final ConsentQueryRepository consentQueryRepository;
  private final ClientDataMapper clientDataMapper;
  private final ConsentDataMapper consentDataMapper;

  /**
   * Retrieves consents for a principal (user) with pagination.
   *
   * @param query the pagination query containing the principal id, page, and limit
   * @return a pageable response containing the consents
   */
  @Transactional(timeout = 2)
  public PageableResponse<ConsentResponse> getConsents(ConsentsPaginationQuery query) {
    log.info("Trying to get user consents");

    var result =
        consentQueryRepository.findAllByPrincipalId(
            query.getPrincipalId(), query.getPage(), query.getLimit());
    return PageableResponse.<ConsentResponse>builder()
        .page(result.getPage())
        .limit(result.getLimit())
        .data(
            StreamSupport.stream(result.getData().spliterator(), false)
                .map(
                    clientConsent ->
                        consentDataMapper.toConsentResponse(
                            clientConsent.getConsent(),
                            clientDataMapper.toClientInfoResponse(clientConsent.getClient())))
                .collect(Collectors.toCollection(LinkedHashSet::new)))
        .next(result.getNext())
        .previous(result.getPrevious())
        .build();
  }
}
