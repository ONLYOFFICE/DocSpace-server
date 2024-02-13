package com.asc.authorization.web.security.crypto.jwks;

import com.asc.authorization.core.entities.KeyPair;
import com.asc.authorization.core.entities.KeyPairType;
import com.asc.authorization.core.usecases.service.key.KeyPairCreationUsecases;
import com.asc.authorization.core.usecases.service.key.KeyPairRetrieveUsecases;
import com.asc.authorization.web.server.utilities.KeyPairMapper;
import com.nimbusds.jose.KeySourceException;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSelector;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import jakarta.annotation.PostConstruct;
import lombok.AllArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.security.oauth2.jose.jws.SignatureAlgorithm;
import org.springframework.security.oauth2.server.authorization.token.JwtEncodingContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenCustomizer;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;

@Slf4j
@Component
@AllArgsConstructor
public class KeyPairRepositoryJWKSource implements JWKSource<SecurityContext>,
        OAuth2TokenCustomizer<JwtEncodingContext> {
    @Autowired
    @Qualifier("ec")
    private JwksKeyPairGenerator keyPairGenerator;
    private final KeyPairMapper keyPairMapper;

    private final KeyPairRetrieveUsecases retrieveUsecases;
    private final KeyPairCreationUsecases creationUsecases;

    private List<KeyPair> keyPairs = new ArrayList<>();

    @PostConstruct
    public void init() throws Exception {
        keyPairs.addAll(retrieveUsecases.findAll()
                .stream().filter(p -> p.getPairType().equals(keyPairGenerator.type()))
                .filter(p -> {
                    try {
                        keyPairGenerator
                                .buildKey(p.getId(), p.getPublicKey(), p.getPrivateKey());
                        return true;
                    } catch (Exception e) {
                        return false;
                    }
                })
                .filter(Objects::nonNull)
                .collect(Collectors.toList()));
        if (keyPairs.size() < 1) {
            MDC.put("type", keyPairGenerator.type().name());
            log.debug("Found no certificates of this type. Generating a new one");
            MDC.clear();
            var pair = keyPairGenerator.generateKeyPair();
            keyPairs.add(creationUsecases.save(KeyPair
                    .builder()
                    .publicKey(keyPairMapper.toString(pair.getPublic()))
                    .privateKey(keyPairMapper.toString(pair.getPrivate()))
                    .pairType(keyPairGenerator.type())
                    .build()));
        }
    }


    @SneakyThrows
    public List<JWK> get(JWKSelector jwkSelector, SecurityContext securityContext)
            throws KeySourceException {
        log.debug("Trying to get JWK");
        List<JWK> result = new ArrayList<>();
        for (KeyPair keyPair : keyPairs) {
            MDC.put("id", keyPair.getId());
            MDC.put("type", keyPair.getPairType().name());
            log.debug("Validating a key pair");
            if (!keyPair.getPairType().equals(keyPairGenerator.type())) {
                log.debug("Key pair is not supported");
                MDC.clear();
                continue;
            }
            log.debug("Key pair is supported");
            MDC.clear();
            var key = keyPairGenerator.buildKey(keyPair.getId(),
                    keyPair.getPublicKey(), keyPair.getPrivateKey());
            if (jwkSelector.getMatcher().matches(key))
                result.add(key);
        }

        return result;
    }

    public void customize(JwtEncodingContext context) {
        var keyPair = keyPairs.stream().filter(p -> p.getPairType().equals(keyPairGenerator.type()))
                .findFirst().orElseThrow(() -> new UnsupportedOperationException("Could not find any suitable keypair"));

        var principal = context.getPrincipal();
        var authority = principal.getAuthorities().stream().findFirst()
                .orElse(null);
        if (context.getAuthorization().getRegisteredClientId() != null)
            context.getClaims().claim("cid", context.getAuthorization()
                    .getRegisteredClientId());
        if (principal.getDetails() != null)
            context.getClaims()
                    .subject(principal.getDetails().toString());
        if (authority != null)
            context.getClaims()
                    .issuer(String.format("%s/oauth2", authority.getAuthority()))
                    .audience(Arrays.asList(authority.getAuthority()));

        context
                .getJwsHeader()
                .keyId(keyPair.getId())
                .algorithm(keyPair.getPairType()
                        .equals(KeyPairType.EC) ?
                        SignatureAlgorithm.ES256 :
                        SignatureAlgorithm.RS256);
    }
}
