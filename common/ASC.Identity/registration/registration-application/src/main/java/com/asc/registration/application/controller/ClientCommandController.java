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

import com.asc.common.application.proto.AuthorizationServiceGrpc;
import com.asc.common.application.proto.RevokeConsentsRequest;
import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.HttpUtils;
import com.asc.registration.application.security.authentication.BasicSignatureTokenPrincipal;
import com.asc.registration.application.transfer.ChangeClientActivationRequest;
import com.asc.registration.application.transfer.CreateClientRequest;
import com.asc.registration.application.transfer.UpdateClientRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.grpc.StatusRuntimeException;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.ExampleObject;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ProblemDetail;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

/**
 * Controller class for handling client-related commands.
 *
 * <p>This controller provides RESTful endpoints to manage client entities, including creation,
 * update, deletion, activation, and consent revocation. It uses rate limiting to control access and
 * integrates with various services to perform these operations securely.
 */
@Tag(
    name = "OAuth 2.0 / Client Management",
    description =
        "APIs for managing OAuth2 clients including creation, updates, deletion and activation")
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = {
      "${spring.application.web.api}/oauth2/clients",
      "${spring.application.web.api}/clients"
    },
    produces = {MediaType.APPLICATION_JSON_VALUE})
@PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
public class ClientCommandController {
  /** The name of the current service. */
  @Value("${spring.application.name}")
  private String serviceName;

  private final AuthorizationServiceGrpc.AuthorizationServiceBlockingStub
      authorizationServiceClient;

  /** The service for managing client applications. */
  private final ClientApplicationService clientApplicationService;

  private final HttpUtils httpUtils;
  private final AuditMessagePublisher messagePublisher;

  /**
   * Sets logging parameters for the current request, including tenant and user details.
   *
   * @param principal the authenticated principal containing user and tenant information.
   */
  private void setLoggingParameters(BasicSignatureTokenPrincipal principal) {
    MDC.put("tenant_id", String.valueOf(principal.getTenantId()));
    MDC.put("tenant_url", principal.getTenantUrl());
    MDC.put("user_id", principal.getUserId());
    MDC.put("user_name", principal.getUserName());
    MDC.put("user_email", principal.getUserEmail());
  }

  /**
   * Creates a new client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param command the command containing client creation details.
   * @return a {@link ResponseEntity} containing the created client details.
   */
  @RateLimiter(name = "globalRateLimiter")
  @PostMapping(
      produces = {MediaType.APPLICATION_JSON_VALUE},
      consumes = {MediaType.APPLICATION_JSON_VALUE})
  @Operation(
      summary = "Create a new OAuth2 client",
      description =
          "Registers a new OAuth2 client in the caller's tenant and returns it. The body must "
              + "carry a name, a description, a logo and at least one redirect URI, allowed origin "
              + "and scope, and every scope named must already exist in the tenant's scope "
              + "catalogue. Administrators and users may both register clients; the caller is "
              + "recorded as the creator, which is what later restricts a plain user to the clients "
              + "they created. The response is the stored client with its generated client ID and "
              + "secret, and it is the first place either value can be read. Some deployments cap "
              + "how many clients one tenant may hold, and reaching that cap is reported as 400 "
              + "together with the validation failures.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "201",
            description = "Client successfully created",
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
            description =
                "Missing required fields, validation failed, an unknown scope was requested, or "
                    + "the client limit for this tenant has been reached",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to create client",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "415",
            description = "Unsupported media type",
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
  public ResponseEntity<?> createClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @RequestBody
          @Valid
          @Parameter(
              description = "Client creation request containing client details",
              required = true,
              content =
                  @Content(
                      mediaType = MediaType.APPLICATION_JSON_VALUE,
                      schema = @Schema(implementation = CreateClientRequest.class),
                      examples =
                          @ExampleObject(
                              value =
                                  """
                  {
                    "name": "Example Name",
                    "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                    "website_url": "https://example.com",
                    "description": "Example Description",
                    "redirect_uris": ["https://example.com"],
                    "allowed_origins": ["https://example.com"],
                    "logout_redirect_uri": "https://example.com",
                    "terms_url": "https://example.com",
                    "policy_url": "https://example.com",
                    "allow_pkce": false,
                    "scopes": ["files:read", "files:write"]
                  }
                  """)))
          CreateClientRequest command) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.status(HttpStatus.CREATED)
          .body(
              clientApplicationService.createClient(
                  buildAudit(null, request, principal, AuditCode.CREATE_CLIENT),
                  CreateTenantClientCommand.builder()
                      .name(command.getName())
                      .description(command.getDescription())
                      .logo(command.getLogo())
                      .allowPkce(command.isAllowPkce())
                      .isPublic(true)
                      .websiteUrl(command.getWebsiteUrl())
                      .termsUrl(command.getTermsUrl())
                      .policyUrl(command.getPolicyUrl())
                      .redirectUris(command.getRedirectUris())
                      .allowedOrigins(command.getAllowedOrigins())
                      .logoutRedirectUri(command.getLogoutRedirectUri())
                      .scopes(command.getScopes())
                      .tenantId(principal.getTenantId())
                      .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Updates an existing client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param clientId the ID of the client to update.
   * @param command the command containing client update details.
   * @return a {@link ResponseEntity} indicating the status of the update.
   */
  @RateLimiter(name = "globalRateLimiter")
  @PutMapping(
      value = "/{clientId}",
      consumes = {MediaType.APPLICATION_JSON_VALUE})
  @Operation(
      summary = "Update an existing OAuth2 client",
      description =
          "Updates the mutable settings of an existing client and answers 200 with an empty body. "
              + "Only the fields carried in the request body change; the client ID, the secret, the "
              + "tenant and the creator cannot be changed this way. An administrator may update any "
              + "client of the tenant, a plain user only the clients they created, and a client the "
              + "caller may not see is reported as not found rather than as forbidden. The write "
              + "runs under optimistic locking and is retried a few times, so a request that still "
              + "loses the race is rejected with 400 instead of silently overwriting a concurrent "
              + "change. Nothing is returned in the body - read the client back to see the stored "
              + "result.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully updated"),
        @ApiResponse(
            responseCode = "400",
            description =
                "Missing required fields, validation failed, or the client could not be updated "
                    + "because of concurrent modification",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to update client",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "404",
            description =
                "No client with this ID is visible to the caller, or the ID cannot be parsed as a "
                    + "client ID",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "415",
            description = "Unsupported media type",
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
  public ResponseEntity<?> updateClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to update",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId,
      @RequestBody
          @Valid
          @Parameter(
              description = "Client update request containing modified client details",
              required = true,
              content =
                  @Content(
                      mediaType = MediaType.APPLICATION_JSON_VALUE,
                      schema = @Schema(implementation = UpdateClientRequest.class),
                      examples =
                          @ExampleObject(
                              value =
                                  """
                  {
                    "name": "Example Name",
                    "description": "Example Description",
                    "logo": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
                    "allow_pkce": false,
                    "is_public": true,
                    "allowed_origins": ["https://example.com"],
                    "redirect_uris": ["https://example.com/callback"],
                    "scopes": ["files:read", "files:write"]
                  }
                  """)))
          UpdateClientRequest command) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.updateClient(
          buildAudit(clientId, request, principal, AuditCode.UPDATE_CLIENT),
          principal.getRole(),
          UpdateTenantClientCommand.builder()
              .name(command.getName())
              .description(command.getDescription())
              .logo(command.getLogo())
              .allowPkce(command.isAllowPkce())
              .isPublic(true)
              .allowedOrigins(command.getAllowedOrigins())
              .redirectUris(command.getRedirectUris())
              .scopes(command.getScopes())
              .clientId(clientId)
              .tenantId(principal.getTenantId())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Regenerates the secret for a specific client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param clientId the client ID.
   * @return a {@link ResponseEntity} containing the new client secret.
   */
  @RateLimiter(name = "globalRateLimiter")
  @PatchMapping("/{clientId}/regenerate")
  @Operation(
      summary = "Regenerate client secret",
      description =
          "Issues a new secret for the client and returns it. The previous secret stops working "
              + "as soon as this call succeeds, there is no grace period and no way to recover it, "
              + "so every deployed copy of the client has to be updated with the value returned "
              + "here. An administrator may do this for any client of the tenant, a plain user only "
              + "for the clients they created. Tokens already issued to the client keep working; "
              + "only future client authentication is affected. The response carries the new secret "
              + "and nothing else.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Client secret successfully regenerated",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ClientSecretResponse.class),
                    examples =
                        @ExampleObject(
                            value =
                                """
                    {
                      "client_secret": "2c53294c-57fa-4d5b-b9db-e49ebb97f25f"
                    }
                    """))),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank or contains only whitespace",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to regenerate client secret",
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
  public ResponseEntity<ClientSecretResponse> regenerateSecret(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to regenerate secret for",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.regenerateSecret(
              buildAudit(clientId, request, principal, AuditCode.REGENERATE_SECRET),
              principal.getRole(),
              RegenerateTenantClientSecretCommand.builder()
                  .clientId(clientId)
                  .tenantId(principal.getTenantId())
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Revokes the consent for a specific client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param clientId the client ID.
   * @return a {@link ResponseEntity} indicating the status of the revocation.
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping("/{clientId}/revoke")
  @Operation(
      summary = "Revoke client consent",
      description =
          "Revokes the calling user's own consent for one client and answers 200 with an empty "
              + "body. It touches only the caller's grant: other users keep their consents and the "
              + "client itself stays registered. Guests may call it as well as users and "
              + "administrators, because it can never reach anyone else's data. The revocation is "
              + "carried out by the authorization service over gRPC, so a service that reports "
              + "nothing was revoked produces 400 and a service that cannot be reached produces "
              + "503. Once it succeeds the user has to authorize the client again before it can act "
              + "on their behalf.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client consent successfully revoked"),
        @ApiResponse(
            responseCode = "400",
            description =
                "The client ID is blank, or the authorization service reported that the consent "
                    + "was not revoked",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to revoke consent",
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
  @PreAuthorize("hasAnyRole('ADMIN', 'USER', 'GUEST')")
  public ResponseEntity<?> revokeUserClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to revoke consent for",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId) {
    try {
      setLoggingParameters(principal);
      var response =
          authorizationServiceClient.revokeConsents(
              RevokeConsentsRequest.newBuilder()
                  .setPrincipalId(principal.getUserId())
                  .setClientId(clientId)
                  .build());
      if (!response.getSuccess()) return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();

      messagePublisher.publish(
          AuditMessage.builder()
              .ip(httpUtils.extractHostFromUrl(httpUtils.getFirstRequestIP(request)))
              .initiator(serviceName)
              .target(clientId)
              .browser(httpUtils.getClientBrowser(request))
              .platform(httpUtils.getClientOS(request))
              .tenantId(principal.getTenantId())
              .userId(principal.getUserId())
              .userEmail(principal.getUserEmail())
              .userName(principal.getUserName())
              .page(httpUtils.getFullURL(request))
              .action(AuditCode.REVOKE_USER_CLIENT.getCode())
              .build());

      return ResponseEntity.status(HttpStatus.OK).build();
    } catch (StatusRuntimeException e) {
      return ResponseEntity.status(HttpStatus.SERVICE_UNAVAILABLE).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Deletes a specific client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param clientId the client ID.
   * @return a {@link ResponseEntity} indicating the status of the deletion.
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping("/{clientId}")
  @Operation(
      summary = "Delete an OAuth2 client",
      description =
          "Deletes one client from the tenant permanently and answers 200 with an empty body. An "
              + "administrator may delete any client of the tenant, a plain user only the clients "
              + "they created, and a client the caller may not see is reported as not found rather "
              + "than as forbidden. The authorizations and consents issued for the client are "
              + "removed too, but that cleanup is driven by a message and completes on the "
              + "authorization service after this call has already returned. A delete that removes "
              + "no row answers 400. The operation cannot be undone.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank, or the client could not be deleted",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete client",
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
  public ResponseEntity<?> deleteClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to delete",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId) {
    try {
      setLoggingParameters(principal);
      // Note: we are ok with publishing the event and then removing the client
      // without an outbox. It is far more important in out case to remove authorizations
      // and consents on a delete attempt no matter the outcome of that delete
      if (clientApplicationService.deleteClient(
              buildAudit(clientId, request, principal, AuditCode.DELETE_CLIENT),
              principal.getRole(),
              DeleteTenantClientCommand.builder()
                  .clientId(clientId)
                  .tenantId(principal.getTenantId())
                  .build())
          == 1) return ResponseEntity.status(HttpStatus.OK).build();
      return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Deletes all user clients.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @return a {@link ResponseEntity} indicating the status of the deletion.
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping
  @Operation(
      summary = "Delete all user OAuth2 clients",
      description =
          "Deletes every client the calling user created in the current tenant and answers 200 "
              + "with an empty body. The caller's own identity always selects the set, so this "
              + "never reaches clients created by somebody else, not even for an administrator. The "
              + "authorizations and consents of the deleted clients are cleaned up asynchronously "
              + "on the authorization service, and the tenant's client cache is dropped as part of "
              + "the call. Concurrent modification that survives the retries is reported as 400. "
              + "The operation cannot be undone, and the response does not say how many clients "
              + "were removed.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "400",
            description = "The clients could not be deleted because of concurrent modification",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete user clients",
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
  public ResponseEntity<?> deleteUserClients(
      HttpServletRequest request, @AuthenticationPrincipal BasicSignatureTokenPrincipal principal) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.deleteUserClients(
          DeleteUserClientsCommand.builder()
              .tenantId(principal.getTenantId())
              .userId(principal.getUserId())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Deletes all tenant clients.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @return a {@link ResponseEntity} indicating the status of the deletion.
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping("/tenant")
  @Operation(
      summary = "Delete all tenant OAuth2 clients",
      description =
          "Deletes every client registered in the current tenant and answers 200 with an empty "
              + "body. Only an administrator may call it - for a plain user or a guest it is "
              + "refused with 403 - and it removes the clients of all users of the tenant, not only "
              + "those of the caller. The authorizations and consents of the deleted clients are "
              + "cleaned up asynchronously on the authorization service, and the tenant's client "
              + "cache is dropped as part of the call. Concurrent modification that survives the "
              + "retries is reported as 400. The operation cannot be undone, and the response does "
              + "not say how many clients were removed.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "400",
            description = "The clients could not be deleted because of concurrent modification",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete tenant clients",
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
  @PreAuthorize("hasRole('ADMIN')")
  public ResponseEntity<?> deleteTenantClients(
      HttpServletRequest request, @AuthenticationPrincipal BasicSignatureTokenPrincipal principal) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.deleteTenantClients(
          DeleteTenantClientsCommand.builder().tenantId(principal.getTenantId()).build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Changes the activation status of a specific client.
   *
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param clientId the client ID.
   * @param command the command containing activation change details.
   * @return a {@link ResponseEntity} indicating the status of the activation change.
   */
  @RateLimiter(name = "globalRateLimiter")
  @PatchMapping(
      value = "/{clientId}/activation",
      consumes = {MediaType.APPLICATION_JSON_VALUE})
  @Operation(
      summary = "Change client activation status",
      description =
          "Enables or disables an existing client and answers 200 with an empty body. A disabled "
              + "client can no longer obtain new tokens, but the tokens and consents it already "
              + "holds stay valid until they expire on their own: disable a client to stop new "
              + "authorizations, delete it to end the existing ones. An administrator may change "
              + "any client of the tenant, a plain user only the clients they created. The body "
              + "carries the single activation flag, and a client the caller may not see is "
              + "reported as not found rather than as forbidden.",
      tags = {"OAuth 2.0 / Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Client activation status successfully changed"),
        @ApiResponse(
            responseCode = "400",
            description = "The client ID is blank, or the activation status is missing",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to change client activation",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "404",
            description =
                "No client with this ID is visible to the caller, or the ID cannot be parsed as a "
                    + "client ID",
            content = @Content(schema = @Schema(implementation = ProblemDetail.class))),
        @ApiResponse(
            responseCode = "415",
            description = "Unsupported media type",
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
  public ResponseEntity<?> changeActivation(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to change activation for",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotBlank
          String clientId,
      @RequestBody
          @Valid
          @Parameter(
              description = "Client activation change request",
              required = true,
              content =
                  @Content(
                      mediaType = MediaType.APPLICATION_JSON_VALUE,
                      schema = @Schema(implementation = ChangeClientActivationRequest.class),
                      examples =
                          @ExampleObject(
                              value =
                                  """
                  {
                    "status": false
                  }
                  """)))
          ChangeClientActivationRequest command) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.changeActivation(
          buildAudit(clientId, request, principal, AuditCode.CHANGE_CLIENT_ACTIVATION),
          principal.getRole(),
          ChangeTenantClientActivationCommand.builder()
              .clientId(clientId)
              .tenantId(principal.getTenantId())
              .enabled(command.isEnabled())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Builds an audit object containing details about the request and action performed.
   *
   * @param clientId the ID of the client being acted upon, if applicable.
   * @param request the HTTP request.
   * @param principal the authenticated principal.
   * @param auditCode the audit code representing the action performed.
   * @return an {@link Audit} object with details about the action.
   */
  private Audit buildAudit(
      String clientId,
      HttpServletRequest request,
      BasicSignatureTokenPrincipal principal,
      AuditCode auditCode) {
    return Audit.Builder.builder()
        .ip(httpUtils.extractHostFromUrl(httpUtils.getFirstRequestIP(request)))
        .initiator(serviceName)
        .target(clientId)
        .browser(httpUtils.getClientBrowser(request))
        .platform(httpUtils.getClientOS(request))
        .tenantId(principal.getTenantId())
        .userId(principal.getUserId())
        .userEmail(principal.getUserEmail())
        .userName(principal.getUserName())
        .page(httpUtils.getFullURL(request))
        .auditCode(auditCode)
        .build();
  }
}
