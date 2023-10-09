package com.onlyoffice.authorization.api.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.dto.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.dto.request.CreateClientDTO;
import com.onlyoffice.authorization.api.dto.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.dto.response.ClientDTO;
import com.onlyoffice.authorization.api.dto.response.PaginationDTO;
import com.onlyoffice.authorization.api.dto.response.SecretDTO;
import com.onlyoffice.authorization.api.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.usecases.service.client.ClientRetrieveUsecases;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.linkTo;
import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.methodOn;

@Slf4j
@RestController
@RequestMapping(value = "/api/2.0/clients")
@RequiredArgsConstructor
public class ClientController {
    private final String X_TENANT_HEADER = "X-Tenant";
    private List<String> allowedScopes = new ArrayList<>();

    private final ApplicationConfiguration applicationConfiguration;
    private final ClientRetrieveUsecases retrieveUsecases;
    private final ClientCreationUsecases creationUsecases;
    private final ClientMutationUsecases mutationUsecases;
    private final ClientCleanupUsecases cleanupUsecases;

    @PostConstruct
    public void init() {
        this.allowedScopes = applicationConfiguration.getScopes().stream()
                .map(s -> s.getName())
                .collect(Collectors.toList());
    }

    @GetMapping
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<PaginationDTO<ClientDTO>> getClients(
            HttpServletResponse response,
            @RequestParam(value = "page") @Min(value = 0) int page,
            @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new get clients request for tenant {} with page {} and limit", tenant, page, limit);

        PaginationDTO<ClientDTO> pagination = retrieveUsecases.getTenantClients(tenant, page, limit);
        for (final ClientDTO client : pagination.getData()) {
            client.add(linkTo(methodOn(ClientController.class)
                    .getClient(response, client.getClientId()))
                    .withRel(HttpMethod.GET.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("get_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .updateClient(response, client.getClientId(), null))
                    .withRel(HttpMethod.PUT.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("update_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .deleteClient(response, client.getClientId()))
                    .withRel(HttpMethod.DELETE.name())
                    .withTitle("delete_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .regenerateSecret(response, client.getClientId()))
                    .withRel(HttpMethod.PATCH.name())
                    .withTitle("regenerate_secret"));
            client.add(linkTo(methodOn(ClientController.class)
                    .activateClient(response, client.getClientId(), null))
                    .withRel(HttpMethod.PATCH.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("activate_client"));
        }

        pagination.add(linkTo(methodOn(ClientController.class)
                .postClient(response, null)).withRel(HttpMethod.POST.name())
                .withTitle("create_client"));

        return ResponseEntity.ok(pagination);
    }

    @GetMapping("/{clientId}")
    @RateLimiter(name = "getClientRateLimiter")
    public ResponseEntity<ClientDTO> getClient(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new get client {} request for tenant {}", clientId, tenant);

        var client = retrieveUsecases.getClient(clientId, tenant);
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(response, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(response, null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(client);
    }

    @PostMapping
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity<ClientDTO> postClient(
            HttpServletResponse response,
            @RequestBody @Valid CreateClientDTO body
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new create client request");
        if (!body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("could not create a new client with the scopes specified");
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        log.info("generating a new client's credentials");
        var client = creationUsecases.clientAsyncCreationTask(body, tenant);
        log.info("successfully submitted a new client broker message");

        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(response, client.getClientId(),null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, client.getClientId(), null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.status(HttpStatus.CREATED).body(client);
    }

    @PutMapping("/{clientId}")
    @RateLimiter(name = "updateClientRateLimiter")
    public ResponseEntity<ClientDTO> updateClient(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid UpdateClientDTO body
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new update client {} request", clientId);
        if (body.getScopes() != null && !body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("Could not update client {} with the scopes specified", clientId);
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        var client = creationUsecases.updateClient(body, clientId, tenant);
        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(response, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .postClient(response, null))
                .withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(client);
    }

    @PatchMapping("/{clientId}/regenerate")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity<SecretDTO> regenerateSecret(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new regenerate client's {} secret request", clientId);
        var regenerate = mutationUsecases.regenerateSecret(clientId, tenant);

        regenerate.add(linkTo(methodOn(ClientController.class)
                .getClient(response, clientId))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .updateClient(response, clientId, null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .deleteClient(response, clientId))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .postClient(response, null)).withRel(HttpMethod.POST.name())
                .withTitle("create_client"));
        regenerate.add(linkTo(methodOn(ClientController.class)
                .activateClient(response, clientId, null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        return ResponseEntity.ok(regenerate);
    }

    @DeleteMapping("/{clientId}")
    @RateLimiter(name = "batchClientRateLimiter")
    public ResponseEntity deleteClient(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new delete client {} request for tenant {}", clientId, tenant);
        cleanupUsecases.clientAsyncDeletionTask(clientId, tenant);
        return ResponseEntity.status(HttpStatus.OK).build();
    }

    @PatchMapping("/{clientId}/activation")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    public ResponseEntity activateClient(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid ChangeClientActivationDTO body
    ) {
        var tenant = Integer.parseInt(response.getHeader(X_TENANT_HEADER));
        log.info("received a new disable client {} request for tenant {}", clientId, tenant);
        if (mutationUsecases.changeActivation(body, clientId))
            return ResponseEntity.status(HttpStatus.OK).build();
        return ResponseEntity.badRequest().build();
    }
}

