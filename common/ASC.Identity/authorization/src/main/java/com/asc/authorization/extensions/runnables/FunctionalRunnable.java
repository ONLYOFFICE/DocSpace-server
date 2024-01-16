package com.asc.authorization.extensions.runnables;

import com.asc.authorization.extensions.functional.CheckedFunction;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.SneakyThrows;

import java.util.function.Consumer;
import java.util.function.Supplier;

/**
 *
 */
@Builder
@AllArgsConstructor
public class FunctionalRunnable implements Runnable {
    private final CheckedFunction<String, String> action;
    private final Supplier<String> extractor;
    private final Consumer<String> setter;

    /**
     *
     */
    @SneakyThrows
    public void run() {
        if (action == null || extractor == null || setter == null)
            return;
        var value = extractor.get();
        if (value == null || value.isBlank())
            return;
        setter.accept(action.apply(value));
    }
}
