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
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.application.transfer.UpdateClientRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.ScopeResponse;
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
import jakarta.validation.constraints.NotEmpty;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
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
    name = "Client Management",
    description =
        "APIs for managing OAuth2 clients including creation, updates, deletion and activation")
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = "${spring.application.web.api}/clients",
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

  /** The service for managing scopes. */
  private final ScopeApplicationService scopeApplicationService;

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
  @PostMapping
  @Operation(
      summary = "Create a new OAuth2 client",
      description =
          "Creates a new OAuth2 client with the specified configuration. "
              + "The client will be created with the provided scopes, redirect URIs, and other settings. "
              + "Returns the created client details including the generated client ID.",
      tags = {"Client Management"},
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
            description = "Invalid request - missing required fields or validation failed",
            content =
                @Content(
                    mediaType = MediaType.APPLICATION_JSON_VALUE,
                    schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to create client",
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
  public ResponseEntity<ClientResponse> createClient(
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
                    "logo": "data:image/png;base64,iVBOR",
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
      if (!scopeApplicationService.getScopes().stream()
          .map(ScopeResponse::getName)
          .collect(Collectors.toSet())
          .containsAll(command.getScopes())) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
      }
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
  @PutMapping("/{clientId}")
  @Operation(
      summary = "Update an existing OAuth2 client",
      description =
          "Updates the configuration of an existing OAuth2 client. "
              + "Allows modification of client name, description, redirect URIs, and other settings. "
              + "The client ID cannot be modified.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully updated"),
        @ApiResponse(
            responseCode = "400",
            description = "Invalid request - missing required fields or validation failed",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to update client",
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
                    "logo": "data:image/png;base64,iVBOR",
                    "allow_pkce": false,
                    "is_public": true,
                    "allowed_origins": ["https://example.com"]
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
          "Generates a new client secret for the specified OAuth2 client. "
              + "The old secret will be immediately invalidated. "
              + "This operation should be used with caution as it requires updating the secret in all client applications.",
      tags = {"Client Management"},
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
            description = "Invalid client ID format",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to regenerate client secret",
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
          "Revokes all user consents for the specified OAuth2 client. "
              + "This will invalidate all access tokens and refresh tokens issued to this client for the current user. "
              + "The user will need to re-authorize the client to access their resources.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client consent successfully revoked"),
        @ApiResponse(
            responseCode = "400",
            description = "Invalid client ID format",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to revoke consent",
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
            responseCode = "503",
            description = "Authorization service unavailable",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error occurred",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class)))
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
          "Permanently deletes an OAuth2 client and all associated data. "
              + "This will invalidate all access tokens and refresh tokens issued to this client. "
              + "This operation cannot be undone.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "400",
            description = "Invalid client ID format",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete client",
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
  public ResponseEntity<?> deleteClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @Parameter(
              description = "ID of the client to delete",
              required = true,
              example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
          @PathVariable
          @NotEmpty
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
          "Permanently deletes user OAuth2 clients and all associated data. "
              + "This will invalidate all access tokens and refresh tokens issued to this client. "
              + "This operation cannot be undone.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete user clients",
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
          "Permanently deletes tenant OAuth2 clients and all associated data. "
              + "This will invalidate all access tokens and refresh tokens issued to this client. "
              + "This operation cannot be undone.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Client successfully deleted"),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to delete tenant clients",
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
  @PatchMapping("/{clientId}/activation")
  @Operation(
      summary = "Change client activation status",
      description =
          "Activates or deactivates an OAuth2 client. "
              + "When deactivated, the client cannot request new access tokens, "
              + "but existing tokens will remain valid until they expire.",
      tags = {"Client Management"},
      security = @SecurityRequirement(name = "x-signature"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Client activation status successfully changed"),
        @ApiResponse(
            responseCode = "400",
            description = "Invalid client ID format or activation status",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "403",
            description = "Insufficient permissions to change client activation",
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
