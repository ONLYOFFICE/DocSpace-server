package com.onlyoffice.authorization.web.server.caching;

public interface DistributedCacheMap<K, V> {
    void put(K key, V value);
    V get(K key);
    void delete(K key);
}
