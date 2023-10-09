package com.onlyoffice.authorization.api.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.dto.response.ScopeDTO;
import com.onlyoffice.authorization.api.exceptions.ScopeNotFoundException;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.annotation.PostConstruct;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Set;
import java.util.stream.Collectors;

@Slf4j
@RestController
@RequestMapping(value = "/api/2.0/scopes")
@RequiredArgsConstructor
public class ScopeController {
    private final ApplicationConfiguration configuration;
    private Set<ScopeDTO> scopes;

    @PostConstruct
    public void init() {
        this.scopes = configuration.getScopes()
                .stream()
                .map(s -> ScopeDTO
                        .builder()
                        .name(s.getName())
                        .description(s.getDescription())
                        .build()
                )
                .collect(Collectors.toSet());
    }

    @GetMapping
    @RateLimiter(name = "getRateLimiter")
    @SneakyThrows
    public ResponseEntity<Iterable<ScopeDTO>> getScopes() {
        log.info("received a request to list scopes");
        return ResponseEntity.ok(this.scopes);
    }

    @GetMapping("/{name}")
    @RateLimiter(name = "getRateLimiter")
    @SneakyThrows
    public ResponseEntity<ScopeDTO> getScope(@PathVariable @NotEmpty String name) {
        log.info("received a get {} scope", name);
        var scope = this.scopes.stream()
                .filter(s -> s.getName().equals(name))
                .findFirst()
                .orElseThrow(() -> new ScopeNotFoundException("could not find scope with this name"));
        return ResponseEntity.ok(scope);
    }
}

