/**
 *
 */
package com.onlyoffice.authorization.core.exceptions;

/**
 *
 */
public class ReadOnlyOperationException extends RuntimeException {
    public ReadOnlyOperationException() {
    }

    public ReadOnlyOperationException(String message) {
        super(message);
    }

    public ReadOnlyOperationException(String message, Throwable cause) {
        super(message, cause);
    }

    public ReadOnlyOperationException(Throwable cause) {
        super(cause);
    }

    public ReadOnlyOperationException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
