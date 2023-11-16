/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.core.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.core.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.core.transfer.response.*;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.external.clients.DocspaceClient;
import com.onlyoffice.authorization.api.external.mappers.ClientMapper;
import com.onlyoffice.authorization.api.security.container.TenantContextContainer;
import com.onlyoffice.authorization.api.security.container.UserContextContainer;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.github.resilience4j.retry.annotation.Retry;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.net.URI;
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
    private final String X_TENANT_HEADER = "X-Tenant";
    private List<String> allowedScopes = new ArrayList<>();

    private final DocspaceClient docspaceClient;
    private final ApplicationConfiguration applicationConfiguration;
    private final ClientRetrieveUsecases retrieveUsecases;
    private final ClientCreationUsecases creationUsecases;
    private final ClientMutationUsecases mutationUsecases;
    private final ClientCleanupUsecases cleanupUsecases;
    private final ConsentRetrieveUsecases consentRetrieveUsecases;
    private final ConsentCleanupUsecases consentCleanupUsecases;

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
            var profile = docspaceClient.getProfile(URI.create(address), cookie, c.getModifiedBy());
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
                    .updateClient(response, address, client.getClientId(), null))
                    .withRel(HttpMethod.PUT.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("update_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .deleteClient(response, address, client.getClientId()))
                    .withRel(HttpMethod.DELETE.name())
                    .withTitle("delete_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .regenerateSecret(response, address, client.getClientId()))
                    .withRel(HttpMethod.PATCH.name())
                    .withTitle("regenerate_secret"));
            client.add(linkTo(methodOn(ClientController.class)
                    .activateClient(response, address, client.getClientId(), null))
                    .withRel(HttpMethod.PATCH.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("activate_client"));
        }

        pagination.add(linkTo(methodOn(ClientController.class)
                .postClient(response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));

        return ResponseEntity.ok(pagination);
    }

    @GetMapping("/{clientId}/info")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<ClientInfoDTO> getClientInfo(@PathVariable @NotEmpty String clientId) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
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
    public ResponseEntity revokeUserClient(@PathVariable @NotEmpty String clientId) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new user revocation request");
        MDC.clear();
        var user = UserContextContainer.context
                .get().getResponse();
        consentCleanupUsecases.asyncRevokeConsent(clientId, user.getEmail());
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
                .updateClient(response, address, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, address, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(client);
    }

    @PostMapping
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity<ClientDTO> postClient(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @RequestBody @Valid CreateClientDTO body
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        log.info("Received a new create client request", body);
        if (!body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("could not create a new client with the scopes specified");
            MDC.clear();
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        log.debug("Generating a new client's credentials");
        var client = creationUsecases.clientAsyncCreationTask(body, context
                .getResponse().getTenantId(), address);
        log.debug("Successfully submitted a new client broker message", client);

        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(response, address, client.getClientId(),null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, address, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, address, client.getClientId(), null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        MDC.clear();

        return ResponseEntity.status(HttpStatus.CREATED).body(client);
    }

    @PutMapping("/{clientId}")
    @Retry(name = "updateClientRetryRateLimiter")
    @RateLimiter(name = "updateClientRateLimiter")
    public ResponseEntity<ClientDTO> updateClient(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid UpdateClientDTO body
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new update client request");
        if (body.getScopes() != null && !body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("Could not update client with the scopes specified");
            MDC.clear();
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        log.debug("Trying to update client with body", body);
        var client = creationUsecases.updateClient(body, clientId, context
                .getResponse().getTenantId());
        log.debug("Client has been updated", client);
        MDC.clear();
        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, address, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, address, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(response, address,null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(client);
    }

    @PatchMapping("/{clientId}/regenerate")
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity<SecretDTO> regenerateSecret(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new regenerate client's secret request");
        log.debug("Trying to regenerate client's secret");
        MDC.clear();
        var regenerate = mutationUsecases.regenerateSecret(clientId, context
                .getResponse().getTenantId());
        log.debug("Regeneration result", regenerate);

        regenerate.add(linkTo(methodOn(ClientController.class)
                .getClient(response, address, clientId))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .updateClient(response, address, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, address, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .postClient(response, address,null)).withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, address, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(regenerate);
    }

    @DeleteMapping("/{clientId}")
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity deleteClient(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new delete client request for tenant");
        MDC.clear();
        cleanupUsecases.clientAsyncDeletionTask(clientId, context.getResponse().getTenantId());
        return ResponseEntity.status(HttpStatus.OK).build();
    }

    @PatchMapping("/{clientId}/activation")
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity activateClient(
            HttpServletResponse response,
            @CookieValue(name = X_DOCSPACE_ADDRESS) String address,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid ChangeClientActivationDTO body
    ) {
        var context = TenantContextContainer.context.get();
        MDC.put("tenantId", String.valueOf(context.getResponse().getTenantId()));
        MDC.put("tenantAlias", context.getResponse().getTenantAlias());
        MDC.put("clientId", clientId);
        log.info("Received a new change client activation request for tenant");
        MDC.clear();
        if (mutationUsecases.changeActivation(body, clientId))
            return ResponseEntity.status(HttpStatus.OK).build();
        return ResponseEntity.badRequest().build();
    }
}

