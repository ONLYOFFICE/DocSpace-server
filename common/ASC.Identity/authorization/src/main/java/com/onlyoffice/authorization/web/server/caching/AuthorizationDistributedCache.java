package com.onlyoffice.authorization.web.server.caching;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.onlyoffice.authorization.web.server.messaging.AuthorizationMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.redisson.api.RedissonClient;
import org.redisson.codec.JsonJacksonCodec;
import org.springframework.stereotype.Component;

import java.util.concurrent.TimeUnit;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AuthorizationDistributedCache implements DistributedCacheMap<String, AuthorizationMessage> {
    private final String AUTHORIZATION_CACHE = "identityAuthorizationCache";
    private final RedissonClient redissonClient;

    private final ObjectMapper objectMapper;

    /**
     *
     * @param key
     * @param message
     */
    public void put(String key, AuthorizationMessage message) {
        var map = redissonClient.<String, AuthorizationMessage>getMapCache(AUTHORIZATION_CACHE,
                new JsonJacksonCodec(objectMapper));
        map.fastPut(key, message, 15, TimeUnit.SECONDS);
        map.destroy();
    }

    /**
     *
     * @param key
     * @return
     */
    public AuthorizationMessage get(String key) {
        var map = redissonClient.<String, AuthorizationMessage>getMapCache(AUTHORIZATION_CACHE,
                new JsonJacksonCodec(objectMapper));
        var msg = map.get(key);
        map.destroy();
        return msg;
    }

    /**
     *
     * @param key
     */
    public void delete(String key) {
        var map = redissonClient.<String, AuthorizationMessage>getMapCache(AUTHORIZATION_CACHE,
                new JsonJacksonCodec(objectMapper));
        map.fastRemove(key);
        map.destroy();
    }
}
