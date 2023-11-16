package com.onlyoffice.authorization.external.caching.hazelcast;

import com.hazelcast.core.HazelcastInstance;
import com.hazelcast.map.IMap;
import com.onlyoffice.authorization.external.caching.configuration.hazelcast.HazelcastCacheAuthorizationMapConfiguration;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.stereotype.Component;

/**
 *
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class AuthorizationCache {
    private final HazelcastInstance hazelcastInstance;

    public OAuth2Authorization put(String key, OAuth2Authorization authorization){
        MDC.put("authorization", key);
        log.info("Adding authorization to the cache");
        MDC.clear();
        IMap<String, OAuth2Authorization> map = hazelcastInstance
                .getMap(HazelcastCacheAuthorizationMapConfiguration.AUTHORIZATIONS);
        return map.putIfAbsent(key, authorization);
    }

    public OAuth2Authorization get(String key){
        MDC.put("authorization", key);
        log.info("Getting authorization from the cache");
        MDC.clear();
        IMap<String, OAuth2Authorization> map = hazelcastInstance
                .getMap(HazelcastCacheAuthorizationMapConfiguration.AUTHORIZATIONS);
        return map.get(key);
    }

    public void delete(String key) {
        MDC.put("authorization", key);
        log.info("Removing authorization from the map");
        MDC.clear();
        IMap<String, OAuth2Authorization> map = hazelcastInstance
                .getMap(HazelcastCacheAuthorizationMapConfiguration.AUTHORIZATIONS);
        map.evict(key);
    }
}
