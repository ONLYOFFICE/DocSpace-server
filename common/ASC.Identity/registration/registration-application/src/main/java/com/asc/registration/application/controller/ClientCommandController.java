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
import com.asc.registration.application.transfer.ChangeTenantClientActivationCommandRequest;
import com.asc.registration.application.transfer.CreateTenantClientCommandRequest;
import com.asc.registration.application.transfer.ErrorResponse;
import com.asc.registration.application.transfer.UpdateTenantClientCommandRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.update.ChangeTenantClientActivationCommand;
import com.asc.registration.service.transfer.request.update.DeleteTenantClientCommand;
import com.asc.registration.service.transfer.request.update.RegenerateTenantClientSecretCommand;
import com.asc.registration.service.transfer.request.update.UpdateTenantClientCommand;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.ScopeResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.grpc.StatusRuntimeException;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
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
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

/**
 * Controller class for handling client-related commands.
 *
 * <p>This controller provides RESTful endpoints to manage client entities, including creation,
 * update, deletion, activation, and consent revocation. It uses rate limiting to control access and
 * integrates with various services to perform these operations securely.
 */
@Tag(name = "Client Command Controller", description = "Command REST API to Manipulate Clients")
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(
    value = "${spring.application.web.api}/clients",
    produces = {MediaType.APPLICATION_JSON_VALUE})
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
    MDC.put("tenant_name", principal.getUserName());
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
      summary = "Creates a new client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(responseCode = "201", description = "Successfully created"),
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
  public ResponseEntity<ClientResponse> createClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @RequestBody @Valid CreateTenantClientCommandRequest command) {
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
      summary = "Updated an existing client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully updated",
            content = @Content),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<?> updateClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotBlank String clientId,
      @RequestBody @Valid UpdateTenantClientCommandRequest command) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.updateClient(
          buildAudit(clientId, request, principal, AuditCode.UPDATE_CLIENT),
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
      summary = "Regenerates the secret for a specific client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(responseCode = "200", description = "Successfully regenerated"),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<ClientSecretResponse> regenerateSecret(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotBlank String clientId) {
    try {
      setLoggingParameters(principal);
      return ResponseEntity.ok(
          clientApplicationService.regenerateSecret(
              buildAudit(clientId, request, principal, AuditCode.REGENERATE_SECRET),
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
      summary = "Revokes the consent for a specific client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthUser"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully revoked",
            content = @Content),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(responseCode = "503", description = "Service unavailable", content = @Content),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<?> revokeUserClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotBlank String clientId) {
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
              .ip(
                  httpUtils
                      .getRequestClientAddress(request)
                      .map(httpUtils::extractHostFromUrl)
                      .orElseGet(
                          () -> httpUtils.extractHostFromUrl(httpUtils.getFirstRequestIP(request))))
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
      summary = "Deletes a specific client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully deleted",
            content = @Content),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<?> deleteClient(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotEmpty String clientId) {
    try {
      setLoggingParameters(principal);
      // Note: we are ok with publishing the event and then removing the client
      // without an outbox. It is far more important in out case to remove authorizations
      // and consents on a delete attempt no matter the outcome of that delete
      clientApplicationService.deleteClient(
          buildAudit(clientId, request, principal, AuditCode.DELETE_CLIENT),
          DeleteTenantClientCommand.builder()
              .clientId(clientId)
              .tenantId(principal.getTenantId())
              .build());
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
      summary = "Changes the activation status of a specific client",
      tags = {"ClientCommandController"},
      security = @SecurityRequirement(name = "ascAuthAdmin"),
      responses = {
        @ApiResponse(
            responseCode = "200",
            description = "Successfully changed activation",
            content = @Content),
        @ApiResponse(
            responseCode = "400",
            description = "Bad request",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "429",
            description = "Too many requests",
            content = @Content(schema = @Schema(implementation = ErrorResponse.class))),
        @ApiResponse(
            responseCode = "500",
            description = "Internal server error",
            content = @Content)
      })
  public ResponseEntity<?> changeActivation(
      HttpServletRequest request,
      @AuthenticationPrincipal BasicSignatureTokenPrincipal principal,
      @PathVariable @NotBlank String clientId,
      @RequestBody @Valid ChangeTenantClientActivationCommandRequest command) {
    try {
      setLoggingParameters(principal);
      clientApplicationService.changeActivation(
          buildAudit(clientId, request, principal, AuditCode.CHANGE_CLIENT_ACTIVATION),
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
        .ip(
            httpUtils
                .getRequestClientAddress(request)
                .map(httpUtils::extractHostFromUrl)
                .orElseGet(
                    () -> httpUtils.extractHostFromUrl(httpUtils.getFirstRequestIP(request))))
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
