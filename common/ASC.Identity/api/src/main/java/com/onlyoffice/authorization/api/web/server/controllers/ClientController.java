/**
 *
 */
package com.onlyoffice.authorization.api.web.server.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.configuration.messaging.RabbitMQConfiguration;
import com.onlyoffice.authorization.api.core.entities.Action;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.security.context.UserContextContainer;
import com.onlyoffice.authorization.api.web.server.transfer.messages.AuditMessage;
import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.*;
import com.onlyoffice.authorization.api.web.server.utilities.HttpUtils;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ClientMapper;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.github.resilience4j.retry.annotation.Retry;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.net.URI;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.linkTo;
import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.methodOn;

/**
 *
 */
@Slf4j
@RestController
@RequestMapping(value = "/api/2.0/clients")
@RequiredArgsConstructor
public class ClientController {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";
    private List<String> allowedScopes = new ArrayList<>();

    private final ApplicationConfiguration applicationConfiguration;
    private final RabbitMQConfiguration configuration;

    private final APIClient apiClient;
    private final AmqpTemplate amqpTemplate;

    private final ClientRetrieveUsecases retrieveUsecases;
    private final ClientCreationUsecases creationUsecases;
    private final ClientMutationUsecases mutationUsecases;
    private final ClientCleanupUsecases cleanupUsecases;
    private final ConsentRetrieveUsecases consentRetrieveUsecases;
    private final ConsentCleanupUsecases consentCleanupUsecases;
    private final AuthorizationCleanupUsecases authorizationCleanupUsecases;

    @PostConstruct
    public void init() {
        this.allowedScopes = applicationConfiguration.getScopes().stream()
                .map(s -> s.getName())
                .collect(Collectors.toList());
    }

    @GetMapping
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<PaginationDTO<ClientDTO>> getClients(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = AUTH_COOKIE_NAME) String ascAuth,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @RequestParam(value = "page") @Min(value = 0) int page,
            @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit
    ) {
        var context = TenantContextContainer.context.get();
        var cookie = String.format("%s=%s", AUTH_COOKIE_NAME, ascAuth);
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("page", String.valueOf(page));
        MDC.put("limit", String.valueOf(limit));
        log.info("Received a new get clients request for tenant with page and limit");
        PaginationDTO<ClientDTO> pagination = retrieveUsecases.getTenantClients(context
                .getResponse().getTenantId(), page, limit);
        log.debug("Got clients");
        MDC.clear();
        pagination.getData().forEach(c -> {
            MDC.put("profile", c.getModifiedBy());
            log.debug("Trying to get profile");
            var profile = apiClient.getProfile(URI.create(address), cookie, c.getModifiedBy());
            if (profile != null && profile.getResponse() != null) {
                log.debug("Got profile");
                var r = profile.getResponse();
                c.setCreatorAvatar(r.getAvatarSmall());
                c.setCreatorDisplayName(String
                        .format("%s %s", r.getFirstName(), r.getLastName()).trim());
            }
            MDC.clear();
        });

        for (final ClientDTO client : pagination.getData()) {
            client.add(linkTo(methodOn(ClientController.class)
                    .getClient(response, address, client.getClientId()))
                    .withRel(HttpMethod.GET.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("get_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .updateClient(request, response, address, client.getClientId(), null))
                    .withRel(HttpMethod.PUT.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("update_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .deleteClient(request, response, address, client.getClientId()))
                    .withRel(HttpMethod.DELETE.name())
                    .withTitle("delete_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .regenerateSecret(request, response, address, client.getClientId()))
                    .withRel(HttpMethod.PATCH.name())
                    .withTitle("regenerate_secret"));
            client.add(linkTo(methodOn(ClientController.class)
                    .activateClient(request, response, address, client.getClientId(), null))
                    .withRel(HttpMethod.PATCH.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("activate_client"));
        }

        pagination.add(linkTo(methodOn(ClientController.class)
                .postClient(request, response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));

        return ResponseEntity.ok(pagination);
    }

    @GetMapping("/{clientId}/info")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<ClientInfoDTO> getClientInfo(@PathVariable @NotEmpty String clientId) {
        MDC.put("clientId", clientId);
        log.info("Received a new get client info request");
        log.debug("Trying to retrieve a client");
        var client = retrieveUsecases.getClient(clientId);
        log.debug("Found a client", client);
        MDC.clear();
        return ResponseEntity.ok(ClientMapper.INSTANCE.fromClientToInfoDTO(client));
    }

    @DeleteMapping("/{clientId}/revoke")
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity revokeUserClient(
            HttpServletRequest request,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new user revocation request");
        MDC.clear();
        var user = UserContextContainer.context
                .get().getResponse();
        consentCleanupUsecases.asyncRevokeConsent(clientId, user.getEmail());
        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.REVOKE_USER_CLIENT)
                        .build());
        return ResponseEntity.status(HttpStatus.OK).build();
    }

    @GetMapping("/consents")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<Set<ConsentDTO>> getClientsInfo() {
        var context = TenantContextContainer.context.get();
        var user = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("user", user.getResponse().getUserName());
        log.info("Received a new get clients info");
        log.debug("Trying to retrieve all clients by principal name");
        var result = consentRetrieveUsecases
                .getAllByPrincipalName(user.getResponse().getEmail());
        MDC.put("number of clients", String.valueOf(result.size()));
        log.debug("Found clients");
        MDC.clear();
        return ResponseEntity.ok(result);
    }

    @GetMapping("/{clientId}")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<ClientDTO> getClient(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("clientId", clientId);
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        log.info("Received a new get client request for tenant");
        log.debug("Trying to retrieve client");
        MDC.clear();
        var client = retrieveUsecases.getClient(clientId);
        log.debug("Found client", client);
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(null, response, address, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(null, response, address, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(null, response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(null, response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(null, response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(client);
    }

    @PostMapping
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity<ClientDTO> postClient(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @RequestBody @Valid CreateClientDTO body
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        log.info("Received a new create client request", body);
        if (!body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("could not create a new client with the scopes specified");
            MDC.clear();
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        log.debug("Generating a new client's credentials");
        var client = creationUsecases.createClientAsync(body, tenantContext
                .getResponse().getTenantId(), address);
        log.debug("Successfully submitted a new client broker message", client);

        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(request, response, address, client.getClientId(),null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(request, response, address, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(request, response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(request, response, address, client.getClientId(), null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        MDC.clear();
        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.CREATE_CLIENT)
                        .build());

        return ResponseEntity.status(HttpStatus.CREATED).body(client);
    }

    @PutMapping("/{clientId}")
    @Retry(name = "updateClientRetryRateLimiter")
    @RateLimiter(name = "updateClientRateLimiter")
    public ResponseEntity<ClientDTO> updateClient(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid UpdateClientDTO body
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new update client request");
        log.debug("Trying to update client with body", body);
        var client = creationUsecases.updateClient(body, clientId, tenantContext
                .getResponse().getTenantId());
        log.debug("Client has been updated", client);
        MDC.clear();
        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(request, response, address, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(request, response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(request, response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(request, response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.UPDATE_CLIENT)
                        .build());

        return ResponseEntity.ok(client);
    }

    @PatchMapping("/{clientId}/regenerate")
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity<SecretDTO> regenerateSecret(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new regenerate client's secret request");
        log.debug("Trying to regenerate client's secret");
        MDC.clear();
        authorizationCleanupUsecases.deleteAuthorizationsByClientId(clientId);
        var regenerate = mutationUsecases.regenerateSecret(clientId, tenantContext
                .getResponse().getTenantId());
        log.debug("Regeneration result", regenerate);

        regenerate.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, clientId))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .updateClient(request, response, address, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .deleteClient(request, response, address, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .postClient(request, response, address,null)).withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .activateClient(request, response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.REGENERATE_SECRET)
                        .build());

        return ResponseEntity.ok(regenerate);
    }

    @DeleteMapping("/{clientId}")
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity deleteClient(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new delete client request for tenant");
        MDC.clear();
        cleanupUsecases.deleteClientAsync(clientId, tenantContext.getResponse().getTenantId());
        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.DELETE_CLIENT)
                        .build());
        return ResponseEntity.status(HttpStatus.OK).build();
    }

    @PatchMapping("/{clientId}/activation")
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity activateClient(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid ChangeClientActivationDTO body
    ) {
        var tenantContext = TenantContextContainer.context.get();
        var userContext = UserContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(tenantContext.getResponse().getTenantId()));
        MDC.put("tenantAlias", tenantContext.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new change client activation request for tenant");
        MDC.clear();
        amqpTemplate.convertAndSend(
                configuration.getAudit().getExchange(),
                configuration.getAudit().getRouting(),
                AuditMessage.builder()
                        .ip(HttpUtils.getRequestIP(request))
                        .browser(HttpUtils.getClientBrowser(request))
                        .platform(HttpUtils.getClientOS(request))
                        .date(Timestamp.from(Instant.now()))
                        .tenantId(tenantContext.getResponse().getTenantId())
                        .userId(userContext.getResponse().getId())
                        .page(HttpUtils.getFullURL(request))
                        .actionEnum(Action.CHANGE_CLIENT_ACTIVATION)
                        .build());
        if (mutationUsecases.changeActivation(body, clientId))
            return ResponseEntity.status(HttpStatus.OK).build();
        return ResponseEntity.badRequest().build();
    }
}

