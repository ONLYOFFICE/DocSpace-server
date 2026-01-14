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
    name = "Client Querying",
    description = "APIs for retrieving OAuth2 client information and user consents")
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
      summary = "Get client details",
      description =
          "Retrieves detailed information about a specific OAuth2 client "
              + "including its name, description, redirect URIs, and scopes.",
      tags = {"Client Querying"},
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
                      "logo": "data:image/png;base64,ivBOR",
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
            description = "Invalid client ID format",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to view client",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "404",
            description = "Client not found",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class)))
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
          "Retrieves a paginated list of OAuth2 clients. "
              + "The results can be paginated using the limit parameter and last seen client ID/creation date.",
      tags = {"Client Querying"},
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
                          "logo": "data:image/png;base64,ivBOR",
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
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to list clients",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests - rate limit exceeded",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class)))
      })
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<PageableResponse<ClientResponse>> getClients(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(description = "Pagination limit", required = true, example = "1")
          @RequestParam(value = "limit", defaultValue = "30")
          @Min(value = 1)
          @Max(value = 50)
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
      summary = "Retrieves detailed information for a specific client",
      tags = {"Client Querying"},
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
                                  "logo": "data:image/png;base64,ivBOR",
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
      summary = "Handles the GET request for public client information",
      tags = {"Client Querying"},
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
                                  "logo": "data:image/png;base64,ivBOR",
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
      summary = "Retrieves a pageable list of client information",
      tags = {"Client Querying"},
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
                                                  data: [
                                                    {
                                                        "name": "Example Name",
                                                        "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                        "description": "Example Description",
                                                        "website_url": "http://example.com",
                                                        "terms_url": "http://example.com",
                                                        "policy_url": "http://example.com",
                                                        "logo": "data:image/png;base64,ivBOR",
                                                        "authentication_methods": ["client_secret_post"],
                                                        "scopes": ["files:read", "files:write"],
                                                        "is_public": true
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
  @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
  public ResponseEntity<PageableResponse<ClientInfoResponse>> getClientsInfo(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(description = "Pagination limit", required = true, example = "1")
          @RequestParam(value = "limit")
          @Min(value = 1)
          @Max(value = 50)
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
      summary = "Retrieves a pageable list of consents",
      tags = {"Client Querying"},
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
                                                "scopes": [
                                                    "files:read",
                                                    "files:write"
                                                ],
                                                "modified_at": "2024-04-04T12:00:00Z",
                                                "client": {
                                                    "name": "Example Name",
                                                    "client_id": "6c7cf17b-1bd3-47d5-94c6-be2d3570e168",
                                                    "description": "Example Description",
                                                    "website_url": "http://example.com",
                                                    "terms_url": "http://example.com",
                                                    "policy_url": "http://example.com",
                                                    "logo": "data:image/png;base64,ivBOR",
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
                                    """)))
      })
  public ResponseEntity<PageableModificationResponse<ConsentResponse>> getConsents(
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(description = "Pagination limit", required = true, example = "1")
          @RequestParam(value = "limit")
          @Min(value = 1)
          @Max(value = 50)
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
