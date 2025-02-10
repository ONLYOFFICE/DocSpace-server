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

package com.asc.registration.application.controller;

import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.application.security.authentication.BasicSignatureTokenPrincipal;
import com.asc.registration.application.service.ConsentService;
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableModificationResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import java.time.ZonedDateTime;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

/**
 * Controller class for managing client-related queries.
 *
 * <p>This controller provides RESTful endpoints to retrieve client information, including client
 * details, pageable lists of clients, and user consents. It integrates with application services to
 * process and respond to client-related queries.
 */
@Tag(
    name = "Client Query Controller",
    description = "Query REST API to Retrieve Client Information")
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = "${spring.application.web.api}/clients",
    produces = {MediaType.APPLICATION_JSON_VALUE})
public class ClientQueryController {
  private final ClientApplicationService clientApplicationService;
  private final ConsentService consentService;

  /**
   * Sets the logging parameters for the current request.
   *
   * @param principal the authenticated principal containing user and tenant details.
   */
  private void setLoggingParameters(BasicSignatureTokenPrincipal principal) {
    MDC.put("tenant_id", String.valueOf(principal.getTenantId()));
    MDC.put("tenant_url", principal.getTenantUrl());
    MDC.put("user_id", principal.getUserId());
    MDC.put("user_name", principal.getUserName());
    MDC.put("user_email", principal.getUserEmail());
  }

  /**
   * Retrieves the details of a specific client.
   *
   * @param clientId the client ID.
   * @param principal the authenticated principal.
   * @return a {@link ResponseEntity} containing the client details.
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
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClient(
              TenantClientQuery.builder()
                  .clientId(clientId)
                  .tenantId(principal.getTenantId())
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves a pageable list of clients.
   *
   * @param principal the authenticated principal.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the ID of the last client retrieved (optional, for pagination).
   * @param lastCreatedOn the creation date of the last client retrieved (optional, for pagination).
   * @return a {@link ResponseEntity} containing a pageable list of clients.
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
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 50) int limit,
      @RequestParam(value = "last_client_id", required = false) String lastClientId,
      @RequestParam(value = "last_created_on", required = false) ZonedDateTime lastCreatedOn) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClients(
              TenantClientsPaginationQuery.builder()
                  .limit(limit)
                  .lastClientId(lastClientId)
                  .lastCreatedOn(lastCreatedOn)
                  .tenantId(principal.getTenantId())
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves detailed information for a specific client.
   *
   * @param principal the authenticated principal.
   * @param clientId the client ID.
   * @return a {@link ResponseEntity} containing detailed client information.
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
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotBlank String clientId) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClientInfo(
              ClientInfoQuery.builder()
                  .tenantId(principal.getTenantId())
                  .clientId(clientId)
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Handles the GET request for public client information.
   *
   * @param clientId the client ID for which to retrieve public information.
   * @return a {@link ResponseEntity} containing public client information.
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
   * @param principal the authenticated principal.
   * @param limit the maximum number of clients to retrieve.
   * @param lastClientId the ID of the last client retrieved (optional, for pagination).
   * @param lastCreatedOn the creation date of the last client retrieved (optional, for pagination).
   * @return a {@link ResponseEntity} containing a pageable list of client information.
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
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 50) int limit,
      @RequestParam(value = "last_client_id", required = false) String lastClientId,
      @RequestParam(value = "last_created_on", required = false) ZonedDateTime lastCreatedOn) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClientsInfo(
              ClientInfoPaginationQuery.builder()
                  .tenantId(principal.getTenantId())
                  .lastClientId(lastClientId)
                  .lastCreatedOn(lastCreatedOn)
                  .limit(limit)
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Retrieves a pageable list of consents.
   *
   * @param principal the authenticated principal.
   * @param limit the maximum number of consents to retrieve.
   * @param lastModifiedOn the modification date of the last consent retrieved (optional, for
   *     pagination).
   * @return a {@link ResponseEntity} containing a pageable list of consents.
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
  public ResponseEntity<PageableModificationResponse<ConsentResponse>> getConsents(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 50) int limit,
      @RequestParam(value = "last_modified_on", required = false) ZonedDateTime lastModifiedOn) {
    try {
      return ResponseEntity.ok(
          consentService.getConsents(principal.getUserId(), limit, lastModifiedOn));
    } finally {
      MDC.clear();
    }
  }
}
