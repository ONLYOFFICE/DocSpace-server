package com.asc.authorization.api.core.exceptions;

/**
 *
 */
public class EntityCreationException extends RuntimeException {
    public EntityCreationException() {
    }

    public EntityCreationException(String message) {
        super(message);
    }

    public EntityCreationException(String message, Throwable cause) {
        super(message, cause);
    }

    public EntityCreationException(Throwable cause) {
        super(cause);
    }

    public EntityCreationException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
