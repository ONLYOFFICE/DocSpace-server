package com.asc.authorization.web.server.caching;

/**
 *
 * @param <K>
 * @param <V>
 */
public interface DistributedCacheMap<K, V> {
    /**
     *
     * @param key
     * @param value
     */
    void put(K key, V value);

    /**
     *
     * @param key
     * @return
     */
    V get(K key);

    /**
     *
     * @param key
     */
    void delete(K key);
}
