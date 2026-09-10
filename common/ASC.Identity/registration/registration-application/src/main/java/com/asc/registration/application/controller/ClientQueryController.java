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

package com.asc.registration.application.controller;

import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.application.security.authentication.BasicSignatureTokenPrincipal;
import com.asc.registration.application.service.ConsentService;
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
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.ExampleObject;
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
import org.springframework.http.ProblemDetail;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
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
    name = "OAuth 2.0 / Client Querying",
    description = "APIs for retrieving OAuth2 client information and user consents")
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = {
      "${spring.application.web.api}/oauth2/clients",
      "${spring.application.web.api}/clients"
    },
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
      summary = "Get client details",
      description =
          "Returns the whole stored record of one client: its name and description, its secret, "
              + "scopes, redirect URIs, allowed origins, logout redirect URIs and audit fields. An "
              + "administrator sees any client of the tenant, a plain user only the clients they "
              + "created, and a guest none of them. Whatever the caller may not see is reported as "
              + "404 rather than 403, so absence and lack of access are deliberately "
              + "indistinguishable, and an identifier that is not a valid client ID is reported the "
              + "same way. The response is a single object, not a collection.",
      tags = {"OAuth 2.0 / Client Querying"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Client details successfully retrieved",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ClientResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                    {
                      "name": "Example Name",
                      "description": "Example Description",
                      "tenant": 1,
                      "scopes": ["files:read", "files:write"],
                      "enabled": true,
                      "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                      "client_secret": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                      "website_url": "http://example.com",
                      "terms_url": "http://example.com",
                      "policy_url": "http://example.com",
                      "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                      "authentication_methods": ["client_secret_post"],
                      "redirect_uris": ["https://example.com"],
                      "allowed_origins": ["https://example.com"],
                      "logout_redirect_uris": ["https://example.com"],
                      "created_on": "2024-04-04T12:00:00Z",
                      "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                      "modified_on": "2024-04-04T12:00:00Z",
                      "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                      "is_public": true
                    }
                    """))),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank or contains only whitespace",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to view client",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "404",
            description =
                "No client with this ID is visible to the caller, or the ID cannot be parsed as a "
                    + "client ID",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<ClientResponse> getClient(
      @Parameter(
              description = "ID of the client to retrieve",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClient(
              principal.getRole(),
              TenantClientQuery.builder()
                  .userId(principal.getUserId())
                  .tenantId(principal.getTenantId())
                  .clientId(clientId)
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
      summary = "List clients",
      description =
          "Returns one page of the tenant's clients, newest first, each in the same full form as "
              + "the single-client read. An administrator sees every client of the tenant, a plain "
              + "user only the clients they created. Paging is keyset-based rather than "
              + "offset-based: limit sets the page size, and last_client_id and last_created_on are "
              + "carried over from the previous page to ask for the next one. The limit defaults to "
              + "30 and has to lie between 1 and 50; a value outside that range is rejected with "
              + "400, but a last_created_on that cannot be parsed as a date surfaces as 500 rather "
              + "than 400.",
      tags = {"OAuth 2.0 / Client Querying"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Client list successfully retrieved",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = PageableResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                    {
                      "data": [
                        {
                          "name": "Example Name",
                          "description": "Example Description",
                          "tenant": 1,
                          "scopes": ["files:read", "files:write"],
                          "enabled": true,
                          "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                          "client_secret": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                          "website_url": "http://example.com",
                          "terms_url": "http://example.com",
                          "policy_url": "http://example.com",
                          "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                          "authentication_methods": ["client_secret_post"],
                          "redirect_uris": ["https://example.com"],
                          "allowed_origins": ["https://example.com"],
                          "logout_redirect_uris": ["https://example.com"],
                          "created_on": "2024-04-04T12:00:00Z",
                          "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                          "modified_on": "2024-04-04T12:00:00Z",
                          "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                          "is_public": true
                        }
                      ],
                      "limit": 50,
                      "last_client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                      "last_created_on": "2024-04-04T12:00:00Z"
                    }
                    """))),
        @ApiResponse(
            responseCode = "400",
            description = "Invalid pagination parameters",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to list clients",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<PageableResponse<ClientResponse>> getClients(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description =
                  "How many entries to return, between 1 and 50. Defaults to 30 " + "when omitted.",
              example = "30")
          @RequestParam(value = "limit", defaultValue = "30")
          @Min(value = 1, message = "limit must be at least 1")
          @Max(value = 50, message = "limit must be at most 50")
          int limit,
      @Parameter(
              description = "ID of the last retrieved client",
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @RequestParam(value = "last_client_id", required = false)
          String lastClientId,
      @Parameter(
              description = "Date of the last retrieved client",
              example = "2024-04-04T12:00:00Z")
          @RequestParam(value = "last_created_on", required = false)
          ZonedDateTime lastCreatedOn) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClients(
              principal.getRole(),
              TenantClientsPaginationQuery.builder()
                  .userId(principal.getUserId())
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
      summary = "Get client info",
      description =
          "Retrieves the detailed information for a client with the ID specified in the request. "
              + "It returns the consent-facing subset of the client - name, description, logo, the "
              + "website, terms and policy URLs, authentication methods and scopes - and "
              + "deliberately omits the secret, the redirect URIs and the allowed origins, which is "
              + "what makes it safe to render on a consent screen. An administrator sees any client "
              + "of the tenant, a plain user only the clients they created, and a guest none of "
              + "them. A client the caller may not see is reported as 404, exactly like an unknown "
              + "one.",
      tags = {"OAuth 2.0 / Client Querying"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully retrieved client info",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ClientInfoResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                                {
                                  "name": "Example Name",
                                  "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                  "description": "Example Description",
                                  "website_url": "http://example.com",
                                  "terms_url": "http://example.com",
                                  "policy_url": "http://example.com",
                                  "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                                  "authentication_methods": ["client_secret_post"],
                                  "scopes": ["files:read", "files:write"],
                                  "is_public": true,
                                  "created_on": "2024-04-04T12:00:00Z",
                                  "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                  "modified_on": "2024-04-04T12:00:00Z",
                                  "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"
                                }
                                """))),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank or contains only whitespace",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to view client information",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "404",
            description =
                "No client with this ID is visible to the caller, or the ID cannot be parsed as a "
                    + "client ID",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<ClientInfoResponse> getClientInfo(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to retrieve",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClientInfo(
              principal.getRole(),
              ClientInfoQuery.builder()
                  .userId(principal.getUserId())
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
      summary = "Get public client info",
      description =
          "Returns the same consent-facing client information as the signed read, but without "
              + "requiring a portal signature. It is meant for a login or consent page that has to "
              + "render the client before the user is known, so it resolves the client by ID alone: "
              + "there is no authentication, no tenant scoping and no creator check, and any caller "
              + "who knows a client ID can read that client's public details. It still exposes no "
              + "secret, no redirect URIs and no allowed origins. Being unauthenticated it is "
              + "rate-limited on a separate, tighter budget than the signed endpoints. An unknown "
              + "client ID, and an identifier that is not a client ID at all, are both reported as "
              + "404.",
      tags = {"OAuth 2.0 / Client Querying"},
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully retrieved client public info",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ClientInfoResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                                {
                                  "name": "Example Name",
                                  "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                  "description": "Example Description",
                                  "website_url": "http://example.com",
                                  "terms_url": "http://example.com",
                                  "policy_url": "http://example.com",
                                  "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                                  "authentication_methods": ["client_secret_post"],
                                  "scopes": ["files:read", "files:write"],
                                  "is_public": true,
                                  "created_on": "2024-04-04T12:00:00Z",
                                  "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                  "modified_on": "2024-04-04T12:00:00Z",
                                  "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"
                                }
                                """))),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank or contains only whitespace",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "404",
            description =
                "No client with this ID exists, or the ID cannot be parsed as a client ID",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  public ResponseEntity<ClientInfoResponse> getPublicClientInfo(
      @Parameter(
              description = "ID of the client to retrieve",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId) {
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
      summary = "List client info",
      description =
          "Retrieves a paginated list of information for all clients, each in the same "
              + "consent-facing form as the single-client info read. An administrator sees every "
              + "client of the tenant, a plain user only the clients they created. Paging is "
              + "keyset-based: limit sets the page size, and last_client_id and last_created_on are "
              + "carried over from the previous page. Unlike the full client listing, limit has no "
              + "default here - it has to be supplied on every call and has to lie between 1 and "
              + "50, and a missing or out-of-range value is rejected with 400.",
      tags = {"OAuth 2.0 / Client Querying"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully retrieved clients info",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = PageableResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                                              {
                                                  "data": [
                                                    {
                                                        "name": "Example Name",
                                                        "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                        "description": "Example Description",
                                                        "website_url": "http://example.com",
                                                        "terms_url": "http://example.com",
                                                        "policy_url": "http://example.com",
                                                        "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                                                        "authentication_methods": ["client_secret_post"],
                                                        "scopes": ["files:read", "files:write"],
                                                        "is_public": true,
                                                        "created_on": "2024-04-04T12:00:00Z",
                                                        "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                        "modified_on": "2024-04-04T12:00:00Z",
                                                        "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"
                                                    }
                                                  ],
                                                  "limit": 50,
                                                  "last_client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                  "last_created_on": "2024-04-04T12:00:00Z"
                                              }
                                              """))),
        @ApiResponse(
            responseCode = "400",
            description = "The limit parameter is missing, or is outside the range 1-50",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to list client information",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<PageableResponse<ClientInfoResponse>> getClientsInfo(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description =
                  "How many entries to return, between 1 and 50. It has no default "
                      + "and has to be sent on every call.",
              required = true,
              example = "30")
          @RequestParam(value = "limit")
          @Min(value = 1, message = "limit must be at least 1")
          @Max(value = 50, message = "limit must be at most 50")
          int limit,
      @Parameter(
              description = "ID of the last retrieved client",
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @RequestParam(value = "last_client_id", required = false)
          String lastClientId,
      @Parameter(
              description = "Date of the last retrieved client",
              example = "2024-04-04T12:00:00Z")
          @RequestParam(value = "last_created_on", required = false)
          ZonedDateTime lastCreatedOn) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.getClientsInfo(
              principal.getRole(),
              ClientInfoPaginationQuery.builder()
                  .userId(principal.getUserId())
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
      summary = "List user consents",
      description =
          "Retrieves a paginated list of user consents: the clients the calling user has "
              + "authorized, each with the scopes granted, the moment the consent was last changed "
              + "and the client's consent-facing details. It always reports the caller's own "
              + "consents and nothing else - there is no role check on this endpoint, so guests may "
              + "call it too, and no parameter widens it to another user. The consents are read "
              + "from the authorization service over gRPC, so an authorization service that cannot "
              + "be reached surfaces as 503. Paging is keyset-based on last_modified_on, and limit "
              + "has no default: it has to be supplied on every call and has to lie between 1 and "
              + "50.",
      tags = {"OAuth 2.0 / Client Querying"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully retrieved user consents",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = PageableModificationResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                                    {
                                        "data": [
                                            {
                                                "registered_client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                "scopes": "files:read files:write",
                                                "modified_at": "2024-04-04T12:00:00Z",
                                                "client": {
                                                    "name": "Example Name",
                                                    "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                    "description": "Example Description",
                                                    "website_url": "http://example.com",
                                                    "terms_url": "http://example.com",
                                                    "policy_url": "http://example.com",
                                                    "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                                                    "authentication_methods": [
                                                        "client_secret_post"
                                                    ],
                                                    "scopes": [
                                                        "files:read",
                                                        "files:write"
                                                    ],
                                                    "is_public": true,
                                                    "created_on": "2024-04-04T12:00:00Z",
                                                    "created_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                    "modified_on": "2024-04-04T12:00:00Z",
                                                    "modified_by": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"
                                                }
                                            }
                                        ],
                                        "limit": 50,
                                        "last_modified_on": "2024-04-04T12:00:00Z"
                                    }
                                    """))),
        @ApiResponse(
            responseCode = "400",
            description = "The limit parameter is missing, or is outside the range 1-50",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "The request carries no valid portal signature",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "503",
            description = "Authorization service unavailable",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class)))
      })
  public ResponseEntity<PageableModificationResponse<ConsentResponse>> getConsents(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description =
                  "How many entries to return, between 1 and 50. It has no default "
                      + "and has to be sent on every call.",
              required = true,
              example = "30")
          @RequestParam(value = "limit")
          @Min(value = 1, message = "limit must be at least 1")
          @Max(value = 50, message = "limit must be at most 50")
          int limit,
      @Parameter(
              description = "Date of the last retrieved consent",
              example = "2024-04-04T12:00:00Z")
          @RequestParam(value = "last_modified_on", required = false)
          ZonedDateTime lastModifiedOn) {
    try {
      return ResponseEntity.ok(
          consentService.getConsents(principal.getUserId(), limit, lastModifiedOn));
    } finally {
      MDC.clear();
    }
  }
}
