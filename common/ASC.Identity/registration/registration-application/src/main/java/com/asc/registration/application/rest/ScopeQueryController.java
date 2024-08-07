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

package com.asc.registration.application.rest;

import com.asc.registration.application.security.authentications.AscAuthenticationTokenPrincipal;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.response.ScopeResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import java.util.LinkedHashSet;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Controller class for managing scopes. */
@Slf4j
@RestController
@RequestMapping(value = "${web.api}/scopes")
@RequiredArgsConstructor
public class ScopeQueryController {

  /** The service for managing scopes. */
  private final ScopeApplicationService scopeApplicationService;

  /**
   * Retrieves a list of scopes for the specified tenant.
   *
   * @param principal the authenticated principal containing tenant information
   * @return a response entity containing an iterable of scope responses
   */
  @GetMapping
  @RateLimiter(name = "globalRateLimiter")
  public ResponseEntity<Iterable<ScopeResponse>> getScopes(
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal) {
    MDC.put("tenant_id", String.valueOf(principal.tenant().getTenantId()));
    MDC.put("tenant_alias", principal.tenant().getTenantAlias());
    log.info("Received a request to list scopes");

    var scopes =
        scopeApplicationService.getScopes().stream()
            .map(
                scope ->
                    ScopeResponse.builder()
                        .name(scope.getName())
                        .type(scope.getType())
                        .group(scope.getGroup())
                        .build())
            .sorted(
                (s1, s2) -> {
                  if (s1.getName().equalsIgnoreCase("openid")) return 1;
                  if (s2.getName().equalsIgnoreCase("openid")) return -1;
                  return s1.getName().compareToIgnoreCase(s2.getName());
                })
            .collect(Collectors.toCollection(LinkedHashSet::new));

    MDC.clear();
    return ResponseEntity.ok(scopes);
  }
}
