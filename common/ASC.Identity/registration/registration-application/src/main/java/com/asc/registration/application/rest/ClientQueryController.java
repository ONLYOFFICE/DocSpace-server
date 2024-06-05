package com.asc.registration.application.rest;

import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscResponseWrapper;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.fetch.TenantClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.TenantConsentsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
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
import org.springframework.cache.annotation.Cacheable;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@Slf4j
@RestController
@RequiredArgsConstructor
@RequestMapping(value = "${web.api}/clients")
public class ClientQueryController {
  private final ClientApplicationService clientApplicationService;
  private final AscApiClient ascApiClient;

  private void setLoggingParameters(AscPersonResponse person, AscTenantResponse tenant) {
    MDC.put("tenant_id", String.valueOf(tenant.getTenantId()));
    MDC.put("tenant_name", tenant.getName());
    MDC.put("tenant_alias", tenant.getTenantAlias());
    MDC.put("user_id", person.getId());
    MDC.put("user_name", person.getUserName());
    MDC.put("user_email", person.getEmail());
  }

  @RateLimiter(name = "globalRateLimiter")
  @Cacheable(
      cacheNames = {"clients"},
      key = "#clientId")
  @GetMapping("/{clientId}")
  public ResponseEntity<ClientResponse> getClient(
      @PathVariable @NotBlank String clientId,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings) {
    try {
      setLoggingParameters(person, tenant);
      var zone = ZoneId.of(settings.getTimezone());
      var client =
          clientApplicationService.getClient(
              TenantClientQuery.builder()
                  .clientId(clientId)
                  .tenantId(tenant.getTenantId())
                  .build());
      client.setCreatedOn(client.getCreatedOn().toInstant().atZone(zone));
      client.setModifiedOn(client.getModifiedOn().toInstant().atZone(zone));
      return ResponseEntity.ok(client);
    } finally {
      MDC.clear();
    }
  }

  @RateLimiter(name = "globalRateLimiter")
  @GetMapping
  public ResponseEntity<PageableResponse<ClientResponse>> getClients(
      HttpServletRequest request,
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @RequestParam(value = "page") @Min(value = 0) int page,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit)
      throws ExecutionException, InterruptedException {
    try {
      setLoggingParameters(person, tenant);
      var clients =
          clientApplicationService.getClients(
              TenantClientsPaginationQuery.builder()
                  .limit(limit)
                  .page(page)
                  .tenantId(tenant.getTenantId())
                  .build());
      var tasks = new HashSet<CompletableFuture<AscResponseWrapper<AscPersonResponse>>>();
      clients
          .getData()
          .forEach(
              clientResponse ->
                  tasks.add(
                      (CompletableFuture.supplyAsync(
                          () ->
                              ascApiClient.getProfile(
                                  URI.create(clientResponse.getTenantUrl()),
                                  request.getHeader("Cookie"),
                                  clientResponse.getCreatedBy())))));
      CompletableFuture.allOf(new HashSet<>(tasks).toArray(new CompletableFuture[tasks.size()]));
      var zone = ZoneId.of(settings.getTimezone());
      for (CompletableFuture<AscResponseWrapper<AscPersonResponse>> task : tasks) {
        var response = task.get();
        if (response == null) continue;
        var author = task.get().getResponse();
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

  @RateLimiter(name = "globalRateLimiter")
  @Cacheable(
      cacheNames = {"clients"},
      key = "#clientId")
  @GetMapping("/{clientId}/info")
  public ResponseEntity<ClientInfoResponse> getClientInfo(@PathVariable @NotBlank String clientId) {
    return ResponseEntity.ok(
        clientApplicationService.getClientInfo(
            TenantClientInfoQuery.builder().clientId(clientId).build()));
  }

  @RateLimiter(name = "globalRateLimiter")
  @GetMapping("/consents")
  public ResponseEntity<PageableResponse<ConsentResponse>> getConsents(
      @RequestAttribute("person") AscPersonResponse person,
      @RequestAttribute("tenant") AscTenantResponse tenant,
      @RequestAttribute("settings") AscSettingsResponse settings,
      @RequestParam(value = "page") @Min(value = 0) int page,
      @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit) {
    try {
      setLoggingParameters(person, tenant);
      var zone = ZoneId.of(settings.getTimezone());
      var consents =
          clientApplicationService.getConsents(
              TenantConsentsPaginationQuery.builder()
                  .limit(limit)
                  .page(page)
                  .principalName(person.getEmail())
                  .tenantId(tenant.getTenantId())
                  .build());
      consents.getData().forEach(c -> c.setModifiedOn(c.getModifiedOn().toInstant().atZone(zone)));
      return ResponseEntity.ok(consents);
    } finally {
      MDC.clear();
    }
  }
}
