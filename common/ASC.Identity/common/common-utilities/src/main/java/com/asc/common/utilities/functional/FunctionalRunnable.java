package com.asc.common.utilities.functional;

import java.util.function.Consumer;
import java.util.function.Supplier;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.SneakyThrows;

/**
 * The FunctionalRunnable class represents a functional implementation of the Runnable interface. It
 * encapsulates a function to perform an action on a value extracted by a supplier and set the
 * result using a consumer.
 */
@Builder
@AllArgsConstructor
public class FunctionalRunnable implements Runnable {
  private final CheckedFunction<String, String> action;
  private final Supplier<String> extractor;
  private final Consumer<String> setter;

  /**
   * Executes the functional action encapsulated by this instance.
   *
   * <p>If any of the action, extractor, or setter is null, or the extracted value is null or blank,
   * no action is taken.
   *
   * @throws Exception if an error occurs while executing the functional action
   */
  @SneakyThrows
  public void run() {
    if (action == null || extractor == null || setter == null) return;
    var value = extractor.get();
    if (value == null || value.isBlank()) return;
    setter.accept(action.apply(value));
  }
}
