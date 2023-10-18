/**
 *
 */
package com.onlyoffice.authorization.api.core.exceptions;

/**
 *
 */
public class ScopeNotFoundException extends RuntimeException {
    public ScopeNotFoundException() {
    }

    public ScopeNotFoundException(String message) {
        super(message);
    }

    public ScopeNotFoundException(String message, Throwable cause) {
        super(message, cause);
    }

    public ScopeNotFoundException(Throwable cause) {
        super(cause);
    }

    public ScopeNotFoundException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
