package com.asc.authorization.web.server.ports.services;

import com.asc.authorization.core.entities.KeyPair;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceCleanupUsecases;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceCreationUsecases;
import com.asc.authorization.core.usecases.repositories.key.KeyPairPersistenceRetrievalUsecases;
import com.asc.authorization.core.usecases.service.key.KeyPairCleanupUsecases;
import com.asc.authorization.core.usecases.service.key.KeyPairCreationUsecases;
import com.asc.authorization.core.usecases.service.key.KeyPairRetrieveUsecases;
import com.asc.authorization.web.security.crypto.cipher.Cipher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class KeyPairService implements KeyPairRetrieveUsecases,
        KeyPairCreationUsecases, KeyPairCleanupUsecases {
    private final Cipher cipher;
    private final KeyPairPersistenceRetrievalUsecases retrievalUsecases;
    private final KeyPairPersistenceCreationUsecases creationUsecases;
    private final KeyPairPersistenceCleanupUsecases cleanupUsecases;

    /**
     *
     * @param keyPair
     * @return
     * @throws Exception
     */
    @Transactional(
            timeout = 4000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public KeyPair save(KeyPair keyPair) throws Exception {
        MDC.put("id", keyPair.getId());
        MDC.put("type", keyPair.getPairType().name());
        log.debug("Saving a new key pair");
        MDC.clear();
        var builder = KeyPair
                .builder()
                .pairType(keyPair.getPairType())
                .publicKey(keyPair.getPublicKey())
                .privateKey(keyPair.getPrivateKey())
                .createdAt(keyPair.getCreatedAt());

        keyPair.setPrivateKey(cipher.encrypt(keyPair.getPrivateKey()));
        builder.id(creationUsecases.save(keyPair).getId());

        var pair = builder.build();
        return pair;
    }

    /**
     *
     * @param id
     * @return
     */
    @Transactional(
            timeout = 1500,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class,
            readOnly = true
    )
    public Optional<KeyPair> findById(String id) {
        MDC.put("id", id);
        log.debug("Trying to find a key pair by id");
        MDC.clear();
        return retrievalUsecases.findById(id).map((p) -> {
            try {
                p.setPrivateKey(cipher.decrypt(p.getPrivateKey()));
                return p;
            } catch (Exception e) {
                return null;
            }
        }).filter(Objects::nonNull);
    }

    /**
     *
     * @param publicKey
     * @return
     */
    @Transactional(
            timeout = 1500,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class,
            readOnly = true
    )
    public Optional<KeyPair> findByPublicKey(String publicKey) {
        MDC.put("key", publicKey);
        log.debug("Trying to find a key pair by public key");
        MDC.clear();
        return retrievalUsecases.findByPublicKey(publicKey).map((p) -> {
            try {
                p.setPrivateKey(cipher.decrypt(p.getPrivateKey()));
                return p;
            } catch (Exception e) {
                return null;
            }
        }).filter(Objects::nonNull);
    }

    /**
     *
     * @return
     */
    @Transactional(
            timeout = 4000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class,
            readOnly = true
    )
    public List<KeyPair> findAll() {
        log.debug("Trying to find all key pairs");
        return retrievalUsecases.findAll().stream().map(p -> {
            try {
                p.setPrivateKey(cipher.decrypt(p.getPrivateKey()));
                return p;
            } catch (Exception e) {
                return null;
            }}).filter(Objects::nonNull)
                .collect(Collectors.toList());
    }

    /**
     *
     * @param id
     */
    @Transactional(
            timeout = 4000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void deleteById(String id) {
        MDC.put("id", id);
        log.debug("Trying to remove a key pair by id");
        MDC.clear();
        cleanupUsecases.deleteById(id);
    }

    /**
     *
     * @param publicKey
     */
    @Transactional(
            timeout = 4000,
            isolation = Isolation.REPEATABLE_READ,
            rollbackFor = Exception.class
    )
    public void deleteByPublicKey(String publicKey) {
        MDC.put("key", publicKey);
        log.debug("Trying to remove a key pair by public key");
        MDC.clear();
        cleanupUsecases.deleteByPublicKey(publicKey);
    }
}
