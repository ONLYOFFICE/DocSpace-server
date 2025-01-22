// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.registration.application.service;

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.GetConsentsRequest;
import com.asc.common.core.domain.value.ClientId;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableModificationResponse;
import java.time.ZonedDateTime;
import java.util.Optional;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

/**
 * Service class for managing consents and retrieving client-related information.
 *
 * <p>This service interacts with the authorization service and client application service to fetch
 * and aggregate consent details, including associated client information.
 */
@Service
@RequiredArgsConstructor
public class ConsentService {

  private final AuthorizationServiceGrpc.AuthorizationServiceBlockingStub authorizationService;
  private final ClientApplicationService clientApplicationService;

  /**
   * Retrieves a paginated list of consents for a given principal ID.
   *
   * <p>The method fetches consent data from the authorization service, retrieves associated client
   * information using the {@link ClientApplicationService}, and combines the data into {@link
   * ConsentResponse} objects. If a `lastModifiedOn` timestamp is provided, only consents modified
   * after that timestamp are retrieved.
   *
   * @param principalId the ID of the principal for whom to retrieve consents.
   * @param limit the maximum number of consents to retrieve in a single request.
   * @param lastModifiedOn the timestamp for filtering consents based on their modification date;
   *     can be {@code null}.
   * @return a {@link PageableModificationResponse} containing a list of {@link ConsentResponse}
   *     objects, along with metadata such as the last modification timestamp and limit.
   */
  public PageableModificationResponse<ConsentResponse> getConsents(
      String principalId, int limit, ZonedDateTime lastModifiedOn) {
    var response =
        authorizationService.getConsents(
            GetConsentsRequest.newBuilder()
                .setPrincipalId(principalId)
                .setLimit(limit)
                .setLastModifiedAt(
                    Optional.ofNullable(lastModifiedOn).map(ZonedDateTime::toString).orElse(""))
                .build());

    var clientIds =
        response.getConsentsList().stream()
            .map(c -> new ClientId(UUID.fromString(c.getClientId())))
            .toList();

    var clients = clientApplicationService.getClients(clientIds);

    var consents =
        response.getConsentsList().stream()
            .map(
                consent -> {
                  var client =
                      clients.stream()
                          .filter(c -> c.getClientId().equals(consent.getClientId()))
                          .findFirst();
                  var clientInfo =
                      client
                          .map(
                              dc ->
                                  ClientInfoResponse.builder()
                                      .name(dc.getName())
                                      .clientId(dc.getClientId())
                                      .description(dc.getDescription())
                                      .websiteUrl(dc.getWebsiteUrl())
                                      .termsUrl(dc.getTermsUrl())
                                      .policyUrl(dc.getPolicyUrl())
                                      .logo(dc.getLogo())
                                      .authenticationMethods(dc.getAuthenticationMethods())
                                      .scopes(dc.getScopes())
                                      .isPublic(dc.isPublic())
                                      .createdOn(dc.getCreatedOn())
                                      .createdBy(dc.getCreatedBy())
                                      .modifiedOn(dc.getModifiedOn())
                                      .modifiedBy(dc.getModifiedBy())
                                      .build())
                          .orElse(null);
                  return ConsentResponse.builder()
                      .registeredClientId(consent.getClientId())
                      .scopes(String.join(" ", consent.getScopesList()))
                      .modifiedOn(
                          Optional.of(consent.getModifiedAt())
                              .filter(s -> !s.isEmpty())
                              .map(ZonedDateTime::parse)
                              .orElse(null))
                      .client(clientInfo)
                      .build();
                })
            .toList();

    return PageableModificationResponse.<ConsentResponse>builder()
        .data(consents)
        .lastModifiedOn(
            !response.getLastModifiedAt().isBlank()
                ? ZonedDateTime.parse(response.getLastModifiedAt())
                : null)
        .limit(limit)
        .build();
  }
}
