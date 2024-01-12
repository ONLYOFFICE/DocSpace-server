package com.onlyoffice.authorization.extensions.functional;

/**
 *
 * @param <T>
 * @param <R>
 */
@FunctionalInterface
public interface CheckedFunction<T, R> {
    /**
     *
     * @param t
     * @return
     * @throws Exception
     */
    R apply(T t) throws Exception;
}