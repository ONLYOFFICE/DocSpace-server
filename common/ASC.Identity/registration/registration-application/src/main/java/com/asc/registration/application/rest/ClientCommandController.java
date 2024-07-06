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

import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.HttpUtils;
import com.asc.registration.application.transfer.ChangeTenantClientActivationCommandRequest;
import com.asc.registration.application.transfer.CreateTenantClientCommandRequest;
import com.asc.registration.application.transfer.UpdateTenantClientCommandRequest;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.ScopeResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

/** Controller class for handling client-related commands. */
@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(value = "${web.api}/clients")
public class ClientCommandController {

  /** The service for managing client applications. */
  private final ClientApplicationService clientApplicationService;

  /** The service for managing scopes. */
  private final ScopeApplicationService scopeApplicationService;

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
   * Creates a new client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param command the create client command
   * @return the response entity containing the created client details
   */
  @RateLimiter(name = "globalRateLimiter")
  @PostMapping
  public ResponseEntity<ClientResponse> createClient(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @RequestBody @Valid CreateTenantClientCommandRequest command) {
    try {
      setLoggingParameters(person, tenant);
      if (!scopeApplicationService.getScopes().stream()
          .map(ScopeResponse::getName)
          .collect(Collectors.toSet())
          .containsAll(command.getScopes()))
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
      return ResponseEntity.status(HttpStatus.CREATED)
          .body(
              clientApplicationService.createClient(
                  buildAudit(request, tenant, person, AuditCode.CREATE_CLIENT),
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
                      .tenantId(tenant.getTenantId())
                      .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Updates an existing client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param clientId the client ID
   * @param command the update client command
   * @return the response entity indicating the status of the update
   */
  @RateLimiter(name = "globalRateLimiter")
  @PutMapping("/{clientId}")
  public ResponseEntity<?> updateClient(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @PathVariable @NotBlank String clientId,
      @RequestBody @Valid UpdateTenantClientCommandRequest command) {
    try {
      setLoggingParameters(person, tenant);
      clientApplicationService.updateClient(
          buildAudit(request, tenant, person, AuditCode.UPDATE_CLIENT),
          UpdateTenantClientCommand.builder()
              .name(command.getName())
              .description(command.getDescription())
              .logo(command.getLogo())
              .allowPkce(command.isAllowPkce())
              .isPublic(true)
              .allowedOrigins(command.getAllowedOrigins())
              .clientId(clientId)
              .tenantId(tenant.getTenantId())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Regenerates the secret for a specific client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param clientId the client ID
   * @return the response entity containing the new client secret
   */
  @RateLimiter(name = "globalRateLimiter")
  @PatchMapping("/{clientId}/regenerate")
  public ResponseEntity<ClientSecretResponse> regenerateSecret(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @PathVariable @NotBlank String clientId) {
    try {
      setLoggingParameters(person, tenant);
      return ResponseEntity.ok(
          clientApplicationService.regenerateSecret(
              buildAudit(request, tenant, person, AuditCode.REGENERATE_SECRET),
              RegenerateTenantClientSecretCommand.builder()
                  .clientId(clientId)
                  .tenantId(tenant.getTenantId())
                  .build()));
    } finally {
      MDC.clear();
    }
  }

  /**
   * Revokes the consent for a specific client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param clientId the client ID
   * @return the response entity indicating the status of the revocation
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping("/{clientId}/revoke")
  public ResponseEntity<?> revokeUserClient(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @PathVariable @NotBlank String clientId) {
    try {
      setLoggingParameters(person, tenant);
      clientApplicationService.revokeClientConsent(
          buildAudit(request, tenant, person, AuditCode.REVOKE_USER_CLIENT),
          RevokeClientConsentCommand.builder()
              .clientId(clientId)
              .principalId(person.getId())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Deletes a specific client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param clientId the client ID
   * @return the response entity indicating the status of the deletion
   */
  @RateLimiter(name = "globalRateLimiter")
  @DeleteMapping("/{clientId}")
  public ResponseEntity<?> deleteClient(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @PathVariable @NotEmpty String clientId) {
    try {
      setLoggingParameters(person, tenant);
      clientApplicationService.deleteClient(
          buildAudit(request, tenant, person, AuditCode.DELETE_CLIENT),
          DeleteTenantClientCommand.builder()
              .clientId(clientId)
              .tenantId(tenant.getTenantId())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Changes the activation status of a specific client.
   *
   * @param request the HTTP request
   * @param person the person information
   * @param tenant the tenant information
   * @param settings the settings information
   * @param clientId the client ID
   * @param command the change activation command
   * @return the response entity indicating the status of the activation change
   */
  @RateLimiter(name = "globalRateLimiter")
  @PatchMapping("/{clientId}/activation")
  public ResponseEntity<?> changeActivation(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @PathVariable @NotBlank String clientId,
      @RequestBody @Valid ChangeTenantClientActivationCommandRequest command) {
    try {
      setLoggingParameters(person, tenant);
      clientApplicationService.changeActivation(
          buildAudit(request, tenant, person, AuditCode.CHANGE_CLIENT_ACTIVATION),
          ChangeTenantClientActivationCommand.builder()
              .clientId(clientId)
              .tenantId(tenant.getTenantId())
              .enabled(command.isEnabled())
              .build());
      return ResponseEntity.status(HttpStatus.OK).build();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Builds an audit object for logging purposes.
   *
   * @param request the HTTP request
   * @param tenant the tenant information
   * @param person the person information
   * @param auditCode the audit code
   * @return the audit object
   */
  private Audit buildAudit(
      HttpServletRequest request,
      AscTenantResponse tenant,
      AscPersonResponse person,
      AuditCode auditCode) {
    return Audit.Builder.builder()
        .ip(
            HttpUtils.getRequestClientAddress(request)
                .map(HttpUtils::extractHostFromUrl)
                .orElseGet(
                    () -> HttpUtils.extractHostFromUrl(HttpUtils.getFirstRequestIP(request))))
        .browser(HttpUtils.getClientBrowser(request))
        .platform(HttpUtils.getClientOS(request))
        .tenantId(tenant.getTenantId())
        .userId(person.getId())
        .userEmail(person.getEmail())
        .userName(person.getUserName())
        .page(HttpUtils.getFullURL(request))
        .auditCode(auditCode)
        .build();
  }
}
