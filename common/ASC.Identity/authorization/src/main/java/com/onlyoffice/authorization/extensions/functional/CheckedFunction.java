package com.onlyoffice.authorization.extensions.functional;

/**
 *
 * @param <T>
 * @param <R>
 */
@FunctionalInterface
public interface CheckedFunction<T, R> {
    R apply(T t) throws Exception;
}