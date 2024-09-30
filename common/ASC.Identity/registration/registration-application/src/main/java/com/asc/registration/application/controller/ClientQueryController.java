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

package com.asc.registration.application.controller;

import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscResponseWrapper;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.HttpUtils;
import com.asc.registration.application.security.authentication.AscAuthenticationTokenPrincipal;
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.fetch.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import java.net.URI;
import java.time.ZoneId;
import java.util.HashSet;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

/** Controller class for managing client-related queries. */
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = "${web.api}/clients",
    produces = {MediaType.APPLICATION_JSON_VALUE})
public class ClientQueryController {

  /** The service for managing client applications. */
  private final ClientApplicationService clientApplicationService;

  /** The API client for accessing ASC services. */
  private final AscApiClient ascApiClient;

  /**
   * Sets the logging parameters for the current request.
   *
   * @param person the person information
   * @param tenant the tenant information
   */
  private void setLoggingParameters(AscPersonResponse person, AscTenantResponse tenant) {
    MDC.put("tenant_id", String.valueOf(tenant.getTenantId()));
    MDC.put("tenant_name", tenant.getName());
    MDC.put("tenant_alias", tenant.getTenantAlias());
    MDC.put("user_id", person.getId());
    MDC.put("user_name", person.getUserName());
    MDC.put("user_email", person.getEmail());
  }

  /**
   * Retrieves the details of a specific client.
   *
   * @param clientId the client ID
   * @param principal the authenticated principal
   * @return the response entity containing the client details
   */
  @RateLimiter(name = "globalRateLimiter")
  @GetMapping("/{clientId}")
  @Operation(
      summary = "Retrieves the details of a specific client",
      tags = {"ClientQueryController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully retrieved the client"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<ClientResponse> getClient(
      @PathVariable @NotBlank String clientId,
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal) {
    try {
      setLoggingParameters(principal.me(), principal.tenant());
      var zone = ZoneId.of(principal.settings().getTimezone());
      var client =
          clientApplicationService.getClient(
              TenantClientQuery.builder()
                  .clientId(clientId)
                  .tenantId(principal.tenant().getTenantId())
                  .build());
      client.setCreatedOn(client.getCreatedOn().toInstant().atZone(zone));
      client.setModifiedOn(client.getModifiedOn().toInstant().atZone(zone));
      return ResponseEntity.ok(client);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves a pageable list of clients.
   *
   * @param request the HTTP request
   * @param principal the authenticated principal
   * @param page the page number
   * @param limit the page size
   * @return the response entity containing a pageable list of clients
   * @throws ExecutionException if an execution error occurs
   * @throws InterruptedException if the operation is interrupted
   */
  @RateLimiter(name = "globalRateLimiter")
  @GetMapping
  @Operation(
      summary = "Retrieves a pageable list of clients",
      tags = {"ClientQueryController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully retrieved clients"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<PageableResponse<ClientResponse>> getClients(
      HttpServletRequest request,
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal,
      @RequestParam(value = "page") @Min(value = 0) int page,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit)
      throws ExecutionException, InterruptedException {
    try {
      setLoggingParameters(principal.me(), principal.tenant());
      var clients =
          clientApplicationService.getClients(
              TenantClientsPaginationQuery.builder()
                  .limit(limit)
                  .page(page)
                  .tenantId(principal.tenant().getTenantId())
                  .build());
      var tasks = new HashSet<CompletableFuture<AscResponseWrapper<AscPersonResponse>>>();
      clients
          .getData()
          .forEach(
              clientResponse ->
                  tasks.add(
                      CompletableFuture.supplyAsync(
                          () ->
                              ascApiClient.getProfile(
                                  URI.create(
                                      HttpUtils.getRequestHostAddress(request)
                                          .orElseThrow(
                                              () ->
                                                  new ClientDomainException(
                                                      "Could not retrieve host address from request"))),
                                  request.getHeader("Cookie"),
                                  clientResponse.getCreatedBy()))));
      CompletableFuture.allOf(tasks.toArray(new CompletableFuture[0])).join();
      var zone = ZoneId.of(principal.settings().getTimezone());
      for (CompletableFuture<AscResponseWrapper<AscPersonResponse>> task : tasks) {
        var response = task.get();
        if (response == null) continue;
        var author = response.getResponse();
        if (author == null) continue;
        clients
            .getData()
            .forEach(
                c -> {
                  if (c.getModifiedBy().equals(author.getEmail())) {
                    c.setCreatorAvatar(author.getAvatarSmall());
                    c.setCreatorDisplayName(
                        String.format("%s %s", author.getFirstName(), author.getLastName()).trim());
                  }
                  c.setCreatedOn(c.getCreatedOn().toInstant().atZone(zone));
                  c.setModifiedOn(c.getModifiedOn().toInstant().atZone(zone));
                });
      }
      return ResponseEntity.ok(clients);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves detailed information for a specific client.
   *
   * @param principal the authenticated principal
   * @param clientId the client ID
   * @return the response entity containing the client information
   */
  @RateLimiter(name = "globalRateLimiter")
  @GetMapping("/{clientId}/info")
  @Operation(
      summary = "Retrieves detailed information for a specific client",
      tags = {"ClientQueryController"},
      security = @SecurityRequirement(name = "ascAuth"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully retrieved client info"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<ClientInfoResponse> getClientInfo(
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal,
      @PathVariable @NotBlank String clientId) {
    try {
      setLoggingParameters(principal.me(), principal.tenant());
      return ResponseEntity.ok(
          clientApplicationService.getClientInfo(
              ClientInfoQuery.builder()
                  .tenantId(principal.tenant().getTenantId())
                  .clientId(clientId)
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Handles the GET request for public client information.
   *
   * <p>This endpoint is rate-limited and publicly accessible without authentication. It provides
   * client information for the specified client ID.
   *
   * @param clientId the ID of the client to retrieve information for. Must not be blank.
   * @return a ResponseEntity containing the client information.
   */
  @RateLimiter(name = "publicRateLimiter")
  @GetMapping("/{clientId}/public/info")
  @Operation(
      summary = "Handles the GET request for public client information",
      tags = {"ClientQueryController"},
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully retrieved public client info"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<ClientInfoResponse> getPublicClientInfo(
      @PathVariable @NotBlank String clientId) {
    try {
      return ResponseEntity.ok(clientApplicationService.getClientInfo(clientId));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves a pageable list of client information.
   *
   * @param request the HTTP request
   * @param principal the authenticated principal
   * @param page the page number
   * @param limit the page size
   * @return the response entity containing a pageable list of client information
   */
  @RateLimiter(name = "globalRateLimiter")
  @GetMapping("/info")
  @Operation(
      summary = "Retrieves a pageable list of client information",
      tags = {"ClientQueryController"},
      security = @SecurityRequirement(name = "ascAuth"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully retrieved clients info"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<PageableResponse<ClientInfoResponse>> getClientsInfo(
      HttpServletRequest request,
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal,
      @RequestParam(value = "page") @Min(value = 0) int page,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit) {
    try {
      setLoggingParameters(principal.me(), principal.tenant());
      var clients =
          clientApplicationService.getClientsInfo(
              ClientInfoPaginationQuery.builder()
                  .tenantId(principal.tenant().getTenantId())
                  .page(page)
                  .limit(limit)
                  .build());
      var zone = ZoneId.of(principal.settings().getTimezone());
      clients
          .getData()
          .forEach(
              c -> {
                c.setCreatedOn(c.getCreatedOn().toInstant().atZone(zone));
                c.setModifiedOn(c.getModifiedOn().toInstant().atZone(zone));
              });
      return ResponseEntity.ok(clients);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves a pageable list of consents.
   *
   * @param principal the authenticated principal
   * @param page the page number
   * @param limit the page size
   * @return the response entity containing a pageable list of consents
   */
  @RateLimiter(name = "globalRateLimiter")
  @GetMapping("/consents")
  @Operation(
      summary = "Retrieves a pageable list of consents",
      tags = {"ClientQueryController"},
      security = @SecurityRequirement(name = "ascAuth"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully retrieved user consents"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = {@Content}),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<PageableResponse<ConsentResponse>> getConsents(
      @AuthenticationPrincipal AscAuthenticationTokenPrincipal principal,
      @RequestParam(value = "page") @Min(value = 0) int page,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit) {
    try {
      setLoggingParameters(principal.me(), principal.tenant());
      var zone = ZoneId.of(principal.settings().getTimezone());
      var consents =
          clientApplicationService.getConsents(
              ConsentsPaginationQuery.builder()
                  .limit(limit)
                  .page(page)
                  .principalId(principal.me().getId())
                  .build());
      consents.getData().forEach(c -> c.setModifiedOn(c.getModifiedOn().toInstant().atZone(zone)));
      return ResponseEntity.ok(consents);
    } finally {
      MDC.clear();
    }
  }
}
