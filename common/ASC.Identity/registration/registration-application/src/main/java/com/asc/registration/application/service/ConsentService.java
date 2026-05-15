// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.registration.application.service;

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
 * Service for retrieving and aggregating consent records for a given principal.
 *
 * <p>This service interacts with both the remote authorization service and the client application
 * service to:
 *
 * <ul>
 *   <li>Fetch raw consent data via gRPC.
 *   <li>Retrieve additional client details corresponding to each consent.
 *   <li>Combine the data into a paginated response containing enriched consent information.
 * </ul>
 */
@RequiredArgsConstructor
@Service("registrationConsentService")
public class ConsentService {
  private final GrpcAuthorizationService grpcAuthorizationService;
  private final ClientApplicationService clientApplicationService;

  /**
   * Retrieves a paginated list of consent records for a specific principal.
   *
   * <p>This method performs the following steps:
   *
   * <ol>
   *   <li>Calls the authorization service to obtain consent records using a gRPC request.
   *   <li>Extracts the client identifiers from the retrieved consents.
   *   <li>Fetches client information from the client application service based on those
   *       identifiers.
   *   <li>Maps each consent into a {@link ConsentResponse}, enriching it with the corresponding
   *       client details. The consent's modified timestamp is parsed and assigned if present.
   *   <li>Wraps the resulting list and metadata into a {@link PageableModificationResponse}.
   * </ol>
   *
   * @param principalId the unique identifier of the principal whose consents are being retrieved.
   * @param limit the maximum number of consent records to return in one call.
   * @param lastModifiedOn an optional timestamp used to filter consents; only those modified after
   *     this time are returned. May be {@code null} to indicate no filtering by modification date.
   * @return a {@link PageableModificationResponse} containing:
   *     <ul>
   *       <li>A list of {@link ConsentResponse} objects with enriched client details.
   *       <li>Metadata including the last modification timestamp and the request limit.
   *     </ul>
   */
  public PageableModificationResponse<ConsentResponse> getConsents(
      String principalId, int limit, ZonedDateTime lastModifiedOn) {
    var response = grpcAuthorizationService.getConsents(principalId, limit, lastModifiedOn);
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
