package com.asc.common.utilities.functional;

/**
 * Represents a function that accepts one argument and produces a result.
 *
 * @param <T> the type of the input to the function
 * @param <R> the type of the result of the function
 */
@FunctionalInterface
public interface CheckedFunction<T, R> {
  /**
   * Applies this function to the given argument.
   *
   * @param t the input argument
   * @return the result of the function
   * @throws Exception if an error occurs while applying the function
   */
  R apply(T t) throws Exception;
}
